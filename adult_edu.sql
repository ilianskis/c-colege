-- Hotel management database
DROP DATABASE IF EXISTS hotel_management;
CREATE DATABASE hotel_management CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE hotel_management;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS Rezervare;
DROP TABLE IF EXISTS Client;
DROP TABLE IF EXISTS Camera;

SET FOREIGN_KEY_CHECKS = 1;

CREATE TABLE Camera (
  IdCamera INT NOT NULL AUTO_INCREMENT,
  NumarCamera VARCHAR(20) NOT NULL,
  TipCamera VARCHAR(50) NOT NULL,
  Capacitate INT NOT NULL,
  PretNoapte DECIMAL(10,2) NOT NULL,
  PRIMARY KEY (IdCamera),
  UNIQUE KEY UQ_Camera_NumarCamera (NumarCamera),
  CONSTRAINT CK_Camera_Capacitate CHECK (Capacitate > 0),
  CONSTRAINT CK_Camera_PretNoapte CHECK (PretNoapte > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE Client (
  IdClient INT NOT NULL AUTO_INCREMENT,
  Nume VARCHAR(50) NOT NULL,
  Prenume VARCHAR(50) NOT NULL,
  Telefon VARCHAR(20) NOT NULL,
  SeriaNumarPasaport VARCHAR(50) NOT NULL,
  PRIMARY KEY (IdClient),
  UNIQUE KEY UQ_Client_Pasaport (SeriaNumarPasaport)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE Rezervare (
  IdRezervare INT NOT NULL AUTO_INCREMENT,
  IdCamera INT NOT NULL,
  IdClient INT NOT NULL,
  DataCheckIn DATE NOT NULL,
  DataCheckOut DATE NOT NULL,
  NumarNopti INT NOT NULL,
  CostTotal DECIMAL(10,2) NOT NULL,
  StatusRezervare VARCHAR(20) NOT NULL,
  PRIMARY KEY (IdRezervare),
  KEY IX_Rezervare_Camera (IdCamera),
  KEY IX_Rezervare_Client (IdClient),
  CONSTRAINT FK_Rezervare_Camera FOREIGN KEY (IdCamera) REFERENCES Camera (IdCamera) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK_Rezervare_Client FOREIGN KEY (IdClient) REFERENCES Client (IdClient) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT CK_Rezervare_Date CHECK (DataCheckOut > DataCheckIn),
  CONSTRAINT CK_Rezervare_Nopti CHECK (NumarNopti > 0),
  CONSTRAINT CK_Rezervare_Status CHECK (StatusRezervare IN ('Подтверждено', 'Отменено', 'Завершено'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO Camera (IdCamera, NumarCamera, TipCamera, Capacitate, PretNoapte) VALUES
(1, '101', 'Standard', 2, 550.00),
(2, '102', 'Standard', 1, 450.00),
(3, '201', 'Deluxe', 2, 850.00),
(4, '202', 'Deluxe', 3, 950.00),
(5, '301', 'Suite', 4, 1200.00),
(6, '302', 'Suite', 2, 1100.00),
(7, '401', 'Family', 4, 1350.00),
(8, '402', 'Economy', 1, 350.00);

INSERT INTO Client (IdClient, Nume, Prenume, Telefon, SeriaNumarPasaport) VALUES
(1, 'Иванов', 'Иван', '079111222', 'AB123456'),
(2, 'Смирнова', 'Анна', '+37369111222', 'BC234567'),
(3, 'Попов', 'Дмитрий', '078555666', 'CD345678'),
(4, 'Чебан', 'Мария', '+37368123456', 'DE456789'),
(5, 'Русу', 'Андрей', '069777888', 'EF567890'),
(6, 'Волкова', 'Елена', '079888999', 'FG678901'),
(7, 'Мунтяну', 'Ион', '060123123', 'GH789012'),
(8, 'Ионеску', 'Ольга', '+37367123456', 'HI890123');

INSERT INTO Rezervare (IdRezervare, IdCamera, IdClient, DataCheckIn, DataCheckOut, NumarNopti, CostTotal, StatusRezervare) VALUES
(1, 1, 1, '2026-01-01', '2026-01-03', 2, 1100.00, 'Подтверждено'),
(2, 2, 2, '2026-01-04', '2026-01-06', 2, 900.00, 'Подтверждено'),
(3, 3, 3, '2026-01-07', '2026-01-10', 3, 2550.00, 'Завершено'),
(4, 4, 4, '2026-01-15', '2026-01-18', 3, 2850.00, 'Подтверждено'),
(5, 5, 5, '2026-02-10', '2026-02-15', 5, 6000.00, 'Подтверждено'),
(6, 6, 6, '2026-02-16', '2026-02-20', 4, 4400.00, 'Отменено'),
(7, 7, 7, '2026-03-10', '2026-03-14', 4, 5400.00, 'Завершено'),
(8, 8, 8, '2026-03-20', '2026-03-22', 2, 700.00, 'Подтверждено'),
(9, 1, 4, '2026-01-10', '2026-01-12', 2, 1100.00, 'Завершено'),
(10, 2, 5, '2026-02-01', '2026-02-04', 3, 1350.00, 'Подтверждено'),
(11, 3, 6, '2026-03-01', '2026-03-05', 4, 3400.00, 'Подтверждено'),
(12, 4, 7, '2026-04-01', '2026-04-03', 2, 1900.00, 'Завершено'),
(13, 5, 8, '2026-05-01', '2026-05-04', 3, 3600.00, 'Подтверждено'),
(14, 6, 1, '2026-05-10', '2026-05-12', 2, 2200.00, 'Подтверждено'),
(15, 7, 2, '2026-05-15', '2026-05-18', 3, 4050.00, 'Подтверждено');

ALTER TABLE Camera AUTO_INCREMENT = 9;
ALTER TABLE Client AUTO_INCREMENT = 9;
ALTER TABLE Rezervare AUTO_INCREMENT = 16;
