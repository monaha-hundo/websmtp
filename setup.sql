CREATE DATABASE websmtp;
CREATE USER 'websmtp'@'%' IDENTIFIED BY 'websmtp';
GRANT CREATE, ALTER, DROP, INSERT, UPDATE, DELETE, SELECT, REFERENCES, INDEX, LOCK TABLE on websmtp.* TO 'websmtp'@'%' WITH GRANT OPTION;
FLUSH PRIVILEGES;