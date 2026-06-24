using System.Data.SqlClient;

namespace HotelManagementApp
{
    internal static class DatabaseInitializer
    {
        private static readonly string ConnectionString =
            HotelDb.GetConnectionString();

        public static void Initialize()
        {
            EnsureDatabaseExists();
            EnsureSchemaExists();
            SeedData();
        }

        private static void EnsureDatabaseExists()
        {
            var builder = new SqlConnectionStringBuilder(ConnectionString)
            {
                InitialCatalog = "master"
            };

            using (var connection = new SqlConnection(builder.ConnectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF DB_ID(N'hotel') IS NULL
BEGIN
    CREATE DATABASE [hotel];
END";

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static void EnsureSchemaExists()
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID(N'dbo.Camera', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Camera (
        IdCamera INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Camera PRIMARY KEY,
        NumarCamera NVARCHAR(20) NOT NULL CONSTRAINT UQ_Camera_NumarCamera UNIQUE,
        TipCamera NVARCHAR(50) NOT NULL,
        Capacitate INT NOT NULL CONSTRAINT CK_Camera_Capacitate CHECK (Capacitate > 0),
        PretNoapte DECIMAL(10,2) NOT NULL CONSTRAINT CK_Camera_PretNoapte CHECK (PretNoapte > 0)
    );
END;

IF OBJECT_ID(N'dbo.Client', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Client (
        IdClient INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Client PRIMARY KEY,
        Nume NVARCHAR(50) NOT NULL,
        Prenume NVARCHAR(50) NOT NULL,
        Telefon NVARCHAR(20) NOT NULL,
        SeriaNumarPasaport NVARCHAR(50) NOT NULL CONSTRAINT UQ_Client_Pasaport UNIQUE
    );
END;

IF OBJECT_ID(N'dbo.Rezervare', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rezervare (
        IdRezervare INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rezervare PRIMARY KEY,
        IdCamera INT NOT NULL,
        IdClient INT NOT NULL,
        DataCheckIn DATE NOT NULL,
        DataCheckOut DATE NOT NULL,
        NumarNopti INT NOT NULL,
        CostTotal DECIMAL(10,2) NOT NULL,
        StatusRezervare NVARCHAR(20) NOT NULL,
        CONSTRAINT FK_Rezervare_Camera FOREIGN KEY (IdCamera) REFERENCES dbo.Camera (IdCamera) ON DELETE CASCADE ON UPDATE CASCADE,
        CONSTRAINT FK_Rezervare_Client FOREIGN KEY (IdClient) REFERENCES dbo.Client (IdClient) ON DELETE CASCADE ON UPDATE CASCADE,
        CONSTRAINT CK_Rezervare_Date CHECK (DataCheckOut > DataCheckIn),
        CONSTRAINT CK_Rezervare_Nopti CHECK (NumarNopti > 0),
        CONSTRAINT CK_Rezervare_Status CHECK (StatusRezervare IN (N'ACTIV', N'NEACTIV'))
    );

    CREATE INDEX IX_Rezervare_Camera ON dbo.Rezervare (IdCamera);
    CREATE INDEX IX_Rezervare_Client ON dbo.Rezervare (IdClient);
END";

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static void SeedData()
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM dbo.Camera)
   AND NOT EXISTS (SELECT 1 FROM dbo.Client)
   AND NOT EXISTS (SELECT 1 FROM dbo.Rezervare)
BEGIN
    SET IDENTITY_INSERT dbo.Camera ON;
    INSERT INTO dbo.Camera (IdCamera, NumarCamera, TipCamera, Capacitate, PretNoapte) VALUES
    (1, N'101', N'Standard', 2, 550.00),
    (2, N'102', N'Standard', 1, 450.00),
    (3, N'201', N'Deluxe', 2, 850.00),
    (4, N'202', N'Deluxe', 3, 950.00),
    (5, N'301', N'Suite', 4, 1200.00),
    (6, N'302', N'Suite', 2, 1100.00),
    (7, N'401', N'Family', 4, 1350.00),
    (8, N'402', N'Economy', 1, 350.00);
    SET IDENTITY_INSERT dbo.Camera OFF;

    SET IDENTITY_INSERT dbo.Client ON;
    INSERT INTO dbo.Client (IdClient, Nume, Prenume, Telefon, SeriaNumarPasaport) VALUES
    (1, N'Иванов', N'Иван', N'079111222', N'AB123456'),
    (2, N'Смирнова', N'Анна', N'+37369111222', N'BC234567'),
    (3, N'Попов', N'Дмитрий', N'078555666', N'CD345678'),
    (4, N'Чебан', N'Мария', N'+37368123456', N'DE456789'),
    (5, N'Русу', N'Андрей', N'069777888', N'EF567890'),
    (6, N'Волкова', N'Елена', N'079888999', N'FG678901'),
    (7, N'Мунтяну', N'Ион', N'060123123', N'GH789012'),
    (8, N'Ионеску', N'Ольга', N'+37367123456', N'HI890123');
    SET IDENTITY_INSERT dbo.Client OFF;

    SET IDENTITY_INSERT dbo.Rezervare ON;
    INSERT INTO dbo.Rezervare (IdRezervare, IdCamera, IdClient, DataCheckIn, DataCheckOut, NumarNopti, CostTotal, StatusRezervare) VALUES
    (1, 1, 1, '2026-01-01', '2026-01-03', 2, 1100.00, N'ACTIV'),
    (2, 2, 2, '2026-01-04', '2026-01-06', 2, 900.00, N'ACTIV'),
    (3, 3, 3, '2026-01-07', '2026-01-10', 3, 2550.00, N'NEACTIV'),
    (4, 4, 4, '2026-01-15', '2026-01-18', 3, 2850.00, N'ACTIV'),
    (5, 5, 5, '2026-02-10', '2026-02-15', 5, 6000.00, N'ACTIV'),
    (6, 6, 6, '2026-02-16', '2026-02-20', 4, 4400.00, N'NEACTIV'),
    (7, 7, 7, '2026-03-10', '2026-03-14', 4, 5400.00, N'NEACTIV'),
    (8, 8, 8, '2026-03-20', '2026-03-22', 2, 700.00, N'ACTIV'),
    (9, 1, 4, '2026-01-10', '2026-01-12', 2, 1100.00, N'NEACTIV'),
    (10, 2, 5, '2026-02-01', '2026-02-04', 3, 1350.00, N'ACTIV'),
    (11, 3, 6, '2026-03-01', '2026-03-05', 4, 3400.00, N'ACTIV'),
    (12, 4, 7, '2026-04-01', '2026-04-03', 2, 1900.00, N'NEACTIV'),
    (13, 5, 8, '2026-05-01', '2026-05-04', 3, 3600.00, N'ACTIV'),
    (14, 6, 1, '2026-05-10', '2026-05-12', 2, 2200.00, N'ACTIV'),
    (15, 7, 2, '2026-05-15', '2026-05-18', 3, 4050.00, N'ACTIV');
    SET IDENTITY_INSERT dbo.Rezervare OFF;
END";

                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}
