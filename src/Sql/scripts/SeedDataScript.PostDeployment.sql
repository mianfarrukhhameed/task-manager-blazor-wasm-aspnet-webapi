/*
Post-Deployment Script Template              
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.    
 Use SQLCMD syntax to include a file in the post-deployment script.      
 Example:      :r .\myfile.sql                
 Use SQLCMD syntax to reference a variable in the post-deployment script.    
 Example:      :setvar TableName MyTable              
               SELECT * FROM [$(TableName)]          
--------------------------------------------------------------------------------------
*/
If Not Exists(SELECT * FROM UserProfile WHERE ExternalId = '2ba3faed-ce16-46df-8b95-ab0ef26e8ad6')
BEGIN
SET IDENTITY_INSERT  UserProfile ON
  INSERT INTO UserProfile(Id, ExternalId, [Name], EmailAddress, IsAdmin)
    VALUES(1, '2ba3faed-ce16-46df-8b95-ab0ef26e8ad6','dev','dev@test.com', 0)
  SET IDENTITY_INSERT  UserProfile OFF
END

If Not Exists(SELECT * FROM UserProfile WHERE ExternalId = '1efb2983-09be-47a5-ac2c-bff124d542ec')
BEGIN
SET IDENTITY_INSERT  UserProfile ON
  INSERT INTO UserProfile(Id, ExternalId, [Name], EmailAddress, IsAdmin)
    VALUES(2, '1efb2983-09be-47a5-ac2c-bff124d542ec','admin','admin@test.com', 1)
    SET IDENTITY_INSERT  UserProfile OFF
END