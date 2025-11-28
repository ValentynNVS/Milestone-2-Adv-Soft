CREATE DATABASE FDMS;
GO

USE FDMS;
GO

CREATE TABLE GForceParameters
¨(
	Tail_number VARCHAR(20) NOT NULL,
	Packet_sequence INT NOT NULL,
	Aircraft_timestamp DATETIME NOT NULL,
	Accel_x DECIMAL(10,6) NOT NULL,
	Accel_y DECIMAL(10,6) NOT NULL,
	Accel_z DECIMAL(10,6) NOT NULL,
	Weight DECIMAL(12,4) NOT NULL,
	Stored_timestamp DATETIME DEFAULT GETDATE(),
	PRIMARY KEY (Tail_number, Packet_sequence)
);
GO

CREATE TABLE AttitudeParameters
(
	Tail_number VARCHAR(20) NOT NULL,
	Packet_sequence INT NOT NULL,
	Aircraft_timestamp DATETIME NOT NULL,
	Altitude DECIMAL(12,4) NOT NULL,
	Pitch DECIMAL(10,6) NOT NULL,
	Bank DECIMAL(10,6) NOT NULL,
	Weight DECIMAL(12,4) NOT NULL,
	Stored_timestamp DATETIME DEFAULT GETDATE(),
	PRIMARY KEY (Tail_number, Packet_sequence)
);
GO

CREATE TABLE ErrorLog
(
	Tail_number VARCHAR(20) NOT NULL,
	Packet_sequence INT NOT NULL,
	Raw_packet VARCHAR(MAX) NOT NULL,
	Stored_timestamp DATETIME DEFAULT GETDATE(),
	PRIMARY KEY (Tail_number, Packet_sequence)
);
GO

INSERT INTO GForceParameters (Tail_number, Packet_sequence, Aircraft_timestamp, Accel_x, Accel_y, Accel_z, Weight)
Values ('C-QWWT', 1, GETDATE(), 0.0, 0.0, 0.0, 15000.0000);

INSERT INTO AttitudeParameters (Tail_number, Packet_sequence, Aircraft_timestamp, Altitude, Pitch, Bank, Weight)
Values ('C-QWWT', 1, GETDATE(), 35000.0000, 2.5000, 0.0000, 15000.0000);
GO