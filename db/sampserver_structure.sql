-- MySQL dump 10.13  Distrib 5.5.62, for Win64 (AMD64)
--
-- Host: xxx    Database: sampserver_debug
-- ------------------------------------------------------
-- Server version	5.5.5-10.5.15-MariaDB-0+deb11u1

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `derby_pickups`
--

DROP TABLE IF EXISTS `derby_pickups`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `derby_pickups` (
  `pickup_id` int(11) NOT NULL AUTO_INCREMENT,
  `derby_id` int(11) NOT NULL,
  `pickup_event` int(11) NOT NULL,
  `pickup_model` int(11) NOT NULL,
  `pickup_pos_x` float DEFAULT NULL,
  `pickup_pos_y` float DEFAULT NULL,
  `pickup_pos_z` float DEFAULT NULL,
  PRIMARY KEY (`pickup_id`),
  KEY `derby_pickups_FK` (`derby_id`),
  CONSTRAINT `derby_pickups_FK` FOREIGN KEY (`derby_id`) REFERENCES `derbys` (`derby_id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `derby_spawn`
--

DROP TABLE IF EXISTS `derby_spawn`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `derby_spawn` (
  `spawn_id` int(11) NOT NULL AUTO_INCREMENT,
  `derby_id` int(11) NOT NULL,
  `spawn_pos_x` float NOT NULL,
  `spawn_pos_y` float NOT NULL,
  `spawn_pos_z` float NOT NULL,
  `spawn_rot` float NOT NULL,
  PRIMARY KEY (`spawn_id`),
  KEY `derby_spawnpos_FK` (`derby_id`),
  CONSTRAINT `derby_spawnpos_FK` FOREIGN KEY (`derby_id`) REFERENCES `derbys` (`derby_id`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `derbys`
--

DROP TABLE IF EXISTS `derbys`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `derbys` (
  `derby_id` int(11) NOT NULL AUTO_INCREMENT,
  `derby_name` varchar(100) NOT NULL,
  `derby_creator` varchar(50) NOT NULL,
  `derby_startvehicle` int(11) NOT NULL DEFAULT 509,
  `derby_map` int(11) DEFAULT NULL,
  PRIMARY KEY (`derby_id`),
  KEY `derbys_FK` (`derby_map`),
  CONSTRAINT `derbys_FK` FOREIGN KEY (`derby_map`) REFERENCES `maps` (`map_id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `mapobjects`
--

DROP TABLE IF EXISTS `mapobjects`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `mapobjects` (
  `obj_id` int(11) NOT NULL AUTO_INCREMENT,
  `map_id` int(11) NOT NULL,
  `obj_model` int(11) NOT NULL,
  `obj_pos_x` float NOT NULL,
  `obj_pos_y` float NOT NULL,
  `obj_pos_z` float NOT NULL,
  `obj_rot_x` float NOT NULL,
  `obj_rot_y` float NOT NULL,
  `obj_rot_z` float NOT NULL,
  PRIMARY KEY (`obj_id`),
  KEY `mapobjects_FK` (`map_id`),
  CONSTRAINT `mapobjects_FK` FOREIGN KEY (`map_id`) REFERENCES `maps` (`map_id`)
) ENGINE=InnoDB AUTO_INCREMENT=42 DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `maps`
--

DROP TABLE IF EXISTS `maps`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `maps` (
  `map_id` int(11) NOT NULL AUTO_INCREMENT,
  `map_name` varchar(100) NOT NULL,
  `map_creator` int(11) DEFAULT NULL,
  `map_creationdate` datetime DEFAULT NULL,
  `map_lasteditdate` datetime DEFAULT NULL,
  PRIMARY KEY (`map_id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `parked_vehicles`
--

DROP TABLE IF EXISTS `parked_vehicles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `parked_vehicles` (
  `vehicle_id` int(11) NOT NULL AUTO_INCREMENT,
  `model_id` int(11) NOT NULL,
  `spawn_pos_x` float NOT NULL,
  `spawn_pos_y` float NOT NULL,
  `spawn_pos_z` float NOT NULL,
  `spawn_rot` float NOT NULL,
  `color1` int(11) DEFAULT NULL,
  `color2` int(11) DEFAULT NULL,
  PRIMARY KEY (`vehicle_id`)
) ENGINE=InnoDB AUTO_INCREMENT=33 DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `race_checkpoints`
--

DROP TABLE IF EXISTS `race_checkpoints`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `race_checkpoints` (
  `checkpoint_id` int(11) NOT NULL AUTO_INCREMENT,
  `race_id` int(11) NOT NULL,
  `checkpoint_number` int(11) NOT NULL,
  `checkpoint_pos_x` float NOT NULL,
  `checkpoint_pos_y` float NOT NULL,
  `checkpoint_pos_z` float NOT NULL,
  `checkpoint_size` int(11) NOT NULL,
  `checkpoint_type` int(11) NOT NULL DEFAULT 0,
  `checkpoint_vehiclechange` int(11) DEFAULT NULL,
  `checkpoint_nitro` int(11) DEFAULT NULL,
  PRIMARY KEY (`checkpoint_id`),
  KEY `race_checkpoints_FK` (`race_id`),
  CONSTRAINT `race_checkpoints_FK` FOREIGN KEY (`race_id`) REFERENCES `races` (`race_id`)
) ENGINE=InnoDB AUTO_INCREMENT=768 DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `race_records`
--

DROP TABLE IF EXISTS `race_records`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `race_records` (
  `record_id` int(11) NOT NULL AUTO_INCREMENT,
  `race_id` int(11) NOT NULL,
  `player_id` int(11) NOT NULL,
  `record_duration` time(3) NOT NULL,
  PRIMARY KEY (`record_id`),
  KEY `race_records_FK` (`race_id`),
  KEY `race_records_FK_1` (`player_id`),
  CONSTRAINT `race_records_FK` FOREIGN KEY (`race_id`) REFERENCES `races` (`race_id`),
  CONSTRAINT `race_records_FK_1` FOREIGN KEY (`player_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=68 DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `race_spawn`
--

DROP TABLE IF EXISTS `race_spawn`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `race_spawn` (
  `spawn_id` int(11) NOT NULL AUTO_INCREMENT,
  `race_id` int(11) NOT NULL,
  `spawn_index` int(11) NOT NULL,
  `spawn_pos_x` float NOT NULL,
  `spawn_pos_y` float NOT NULL,
  `spawn_pos_z` float NOT NULL,
  `spawn_rot` float NOT NULL,
  PRIMARY KEY (`spawn_id`),
  KEY `race_spawnpos_FK` (`race_id`),
  CONSTRAINT `race_spawnpos_FK` FOREIGN KEY (`race_id`) REFERENCES `races` (`race_id`)
) ENGINE=InnoDB AUTO_INCREMENT=482 DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `races`
--

DROP TABLE IF EXISTS `races`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `races` (
  `race_id` int(11) NOT NULL AUTO_INCREMENT,
  `race_name` varchar(100) COLLATE latin1_general_ci NOT NULL,
  `race_creator` varchar(50) COLLATE latin1_general_ci NOT NULL,
  `race_type` int(11) NOT NULL DEFAULT 0,
  `race_laps` int(11) NOT NULL DEFAULT 0,
  `race_startvehicle` int(11) NOT NULL DEFAULT 509,
  `race_map` int(11) DEFAULT NULL,
  PRIMARY KEY (`race_id`),
  KEY `races_FK` (`race_map`),
  CONSTRAINT `races_FK` FOREIGN KEY (`race_map`) REFERENCES `maps` (`map_id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=19 DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `users` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(50) COLLATE latin1_general_ci NOT NULL,
  `password` varchar(200) COLLATE latin1_general_ci NOT NULL,
  `adminlvl` int(11) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping routines for database 'sampserver_debug'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2022-06-28  4:05:30
