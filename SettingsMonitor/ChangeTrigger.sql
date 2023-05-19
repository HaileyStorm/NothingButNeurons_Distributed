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



/*
Or, in cases where 'value' can be NULL:
*/

DELIMITER $$

CREATE TRIGGER nodestatus_after_update
AFTER UPDATE
ON NodeStatus FOR EACH ROW
BEGIN
    IF NEW.PID IS NOT NULL THEN
        INSERT INTO SettingsChangeLog (tableName, setting, value, changeTimestamp)
        VALUES ('NodeStatus', OLD.Node, CAST(NEW.PID AS CHAR), NOW());
    ELSE
        INSERT INTO SettingsChangeLog (tableName, setting, value, changeTimestamp)
        VALUES ('NodeStatus', OLD.Node, NULL, NOW());
    END IF;
END$$

DELIMITER ;

DELIMITER $$

CREATE TRIGGER nodestatus_after_insert
AFTER INSERT
ON NodeStatus FOR EACH ROW
BEGIN
    IF NEW.PID IS NOT NULL THEN
        INSERT INTO SettingsChangeLog (tableName, setting, value, changeTimestamp)
        VALUES ('NodeStatus', NEW.Node, CAST(NEW.PID AS CHAR), NOW());
    ELSE
        INSERT INTO SettingsChangeLog (tableName, setting, value, changeTimestamp)
        VALUES ('NodeStatus', NEW.Node, NULL, NOW());
    END IF;
END$$

DELIMITER ;