/*
Add triggers like the following to any new settings table for it to be tracked by SettingsMonitor
*/

DELIMITER $$

CREATE TRIGGER ports_after_update
AFTER UPDATE
ON Ports FOR EACH ROW
BEGIN
    INSERT INTO SettingsChangeLog (tableName, setting, value, changeTimestamp)
    VALUES ('Ports', OLD.node, CAST(NEW.port AS CHAR), NOW());
END$$

DELIMITER ;

DELIMITER $$

CREATE TRIGGER ports_after_insert
AFTER INSERT
ON Ports FOR EACH ROW
BEGIN
    INSERT INTO SettingsChangeLog (tableName, setting, value, changeTimestamp)
    VALUES ('Ports', NEW.node, CAST(NEW.port AS CHAR), NOW());
END$$

DELIMITER ;