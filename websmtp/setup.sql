-- Adminer 4.8.1 MySQL 11.2.3-MariaDB-1:11.2.3+maria~ubu2204 dump

SET NAMES utf8;
SET time_zone = '+00:00';
SET foreign_key_checks = 0;
SET sql_mode = 'NO_AUTO_VALUE_ON_ZERO';

USE `websmtp`;

SET NAMES utf8mb4;

CREATE TABLE `MessageAttachement` (
  `Id` char(36) NOT NULL,
  `Filename` varchar(1000) NOT NULL,
  `MimeType` varchar(255) NOT NULL,
  `Content` longtext NOT NULL,
  `ContentId` varchar(1000) NOT NULL,
  `MessageId` char(36) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_MessageAttachement_MessageId` (`MessageId`),
  CONSTRAINT `FK_MessageAttachement_Messages_MessageId` FOREIGN KEY (`MessageId`) REFERENCES `Messages` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE `Messages` (
  `Id` char(36) NOT NULL,
  `RawMessageId` char(36) NOT NULL,
  `ReceivedOn` datetime NOT NULL,
  `Subject` varchar(1000) NOT NULL,
  `From` varchar(1000) NOT NULL,
  `To` varchar(1000) NOT NULL,
  `TextContent` longtext DEFAULT NULL,
  `HtmlContent` longtext DEFAULT NULL,
  `AttachementsCount` int(11) NOT NULL,
  `Read` tinyint(1) NOT NULL,
  `Deleted` tinyint(1) NOT NULL,
  `Cc` varchar(1000) NOT NULL,
  `Bcc` varchar(1000) NOT NULL,
  `Importance` varchar(8) NOT NULL,
  `DkimFailed` tinyint(1) NOT NULL DEFAULT 0,
  `DmarcFailed` tinyint(1) NOT NULL DEFAULT 0,
  `SpfStatus` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_Messages_RawMessageId` (`RawMessageId`),
  CONSTRAINT `FK_Messages_RawMessages_RawMessageId` FOREIGN KEY (`RawMessageId`) REFERENCES `RawMessages` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE `RawMessages` (
  `Id` char(36) NOT NULL,
  `Content` longblob NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE `Users` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Username` varchar(1000) NOT NULL,
  `PasswordHash` varchar(1000) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES
('20240223140515_Initial2',	'8.0.2'),
('20240229185834_MessageSpamProperties',	'8.0.2');

-- 2024-03-07 22:15:10