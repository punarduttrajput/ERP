using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Host.Migrations
{
    /// <inheritdoc />
    public partial class FixHangfireSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop and recreate with correct Hangfire.MySqlStorage 2.0.x schema.
            // Previous migration used separate columns; library expects Data JSON column.
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS `Hangfire_JobParameter`;
DROP TABLE IF EXISTS `Hangfire_State`;
DROP TABLE IF EXISTS `Hangfire_JobQueue`;
DROP TABLE IF EXISTS `Hangfire_Server`;
DROP TABLE IF EXISTS `Hangfire_Set`;
DROP TABLE IF EXISTS `Hangfire_List`;
DROP TABLE IF EXISTS `Hangfire_AggregatedCounter`;
DROP TABLE IF EXISTS `Hangfire_Counter`;
DROP TABLE IF EXISTS `Hangfire_Job`;
DROP TABLE IF EXISTS `Hangfire_Hash`;
DROP TABLE IF EXISTS `Hangfire_DistributedLock`;

CREATE TABLE `Hangfire_DistributedLock` (`Resource` VARCHAR(100) NOT NULL, `CreatedAt` DATETIME(6) NOT NULL, PRIMARY KEY (`Resource`)) ENGINE=INNODB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Hangfire_Hash` (`Id` BIGINT NOT NULL AUTO_INCREMENT, `Key` VARCHAR(100) NOT NULL, `Field` VARCHAR(100) NOT NULL, `Value` LONGTEXT, `ExpireAt` DATETIME(6) DEFAULT NULL, PRIMARY KEY (`Id`), UNIQUE KEY `IX_Hangfire_Hash_Key_Field` (`Key`,`Field`)) ENGINE=INNODB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Hangfire_Job` (`Id` BIGINT NOT NULL AUTO_INCREMENT, `StateId` BIGINT, `StateName` VARCHAR(20), `InvocationData` LONGTEXT NOT NULL, `Arguments` LONGTEXT NOT NULL, `CreatedAt` DATETIME(6) NOT NULL, `ExpireAt` DATETIME(6), PRIMARY KEY (`Id`), KEY `IX_Hangfire_Job_StateName` (`StateName`)) ENGINE=INNODB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Hangfire_Counter` (`Id` BIGINT NOT NULL AUTO_INCREMENT, `Key` VARCHAR(100) NOT NULL, `Value` INT NOT NULL, `ExpireAt` DATETIME(6) DEFAULT NULL, PRIMARY KEY (`Id`), KEY `IX_Hangfire_Counter_Key` (`Key`)) ENGINE=INNODB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Hangfire_AggregatedCounter` (`Id` BIGINT NOT NULL AUTO_INCREMENT, `Key` VARCHAR(100) NOT NULL, `Value` BIGINT NOT NULL, `ExpireAt` DATETIME(6) DEFAULT NULL, PRIMARY KEY (`Id`), UNIQUE KEY `IX_Hangfire_AggregatedCounter_Key` (`Key`)) ENGINE=INNODB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Hangfire_List` (`Id` BIGINT NOT NULL AUTO_INCREMENT, `Key` VARCHAR(100) NOT NULL, `Value` LONGTEXT, `ExpireAt` DATETIME(6) DEFAULT NULL, PRIMARY KEY (`Id`), KEY `IX_Hangfire_List_Key` (`Key`)) ENGINE=INNODB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Hangfire_Set` (`Id` BIGINT NOT NULL AUTO_INCREMENT, `Key` VARCHAR(100) NOT NULL, `Score` FLOAT NOT NULL, `Value` LONGTEXT NOT NULL, `ExpireAt` DATETIME(6) DEFAULT NULL, PRIMARY KEY (`Id`), UNIQUE KEY `IX_Hangfire_Set_Key_Value` (`Key`,`Value`(255)), KEY `IX_Hangfire_Set_Key_Score` (`Key`,`Score`)) ENGINE=INNODB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Hangfire_Server` (`Id` VARCHAR(100) NOT NULL, `Data` LONGTEXT NULL, `LastHeartbeat` DATETIME(6) NOT NULL, PRIMARY KEY (`Id`)) ENGINE=INNODB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Hangfire_JobQueue` (`Id` BIGINT NOT NULL AUTO_INCREMENT, `JobId` BIGINT NOT NULL, `Queue` VARCHAR(50) NOT NULL, `FetchedAt` DATETIME(6) DEFAULT NULL, `FetchToken` VARCHAR(36) DEFAULT NULL, PRIMARY KEY (`Id`), KEY `IX_Hangfire_JobQueue_JobId` (`JobId`), KEY `IX_Hangfire_JobQueue_Queue_FetchedAt` (`Queue`,`FetchedAt`)) ENGINE=INNODB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Hangfire_State` (`Id` BIGINT NOT NULL AUTO_INCREMENT, `JobId` BIGINT NOT NULL, `Name` VARCHAR(20) NOT NULL, `Reason` VARCHAR(100), `CreatedAt` DATETIME(6) NOT NULL, `Data` LONGTEXT, PRIMARY KEY (`Id`), KEY `IX_Hangfire_State_JobId` (`JobId`)) ENGINE=INNODB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Hangfire_JobParameter` (`Id` BIGINT NOT NULL AUTO_INCREMENT, `JobId` BIGINT NOT NULL, `Name` VARCHAR(40) NOT NULL, `Value` LONGTEXT, PRIMARY KEY (`Id`), KEY `IX_Hangfire_JobParameter_JobId` (`JobId`)) ENGINE=INNODB DEFAULT CHARSET=utf8mb4;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS `Hangfire_JobParameter`;
DROP TABLE IF EXISTS `Hangfire_State`;
DROP TABLE IF EXISTS `Hangfire_JobQueue`;
DROP TABLE IF EXISTS `Hangfire_Server`;
DROP TABLE IF EXISTS `Hangfire_Set`;
DROP TABLE IF EXISTS `Hangfire_List`;
DROP TABLE IF EXISTS `Hangfire_AggregatedCounter`;
DROP TABLE IF EXISTS `Hangfire_Counter`;
DROP TABLE IF EXISTS `Hangfire_Job`;
DROP TABLE IF EXISTS `Hangfire_Hash`;
DROP TABLE IF EXISTS `Hangfire_DistributedLock`;
");
        }
    }
}
