-- Demo accounts only. Password: Demo@12345. Existing non-null passwords are preserved.
USE [CloneEbayBuyer];
GO
SET XACT_ABORT ON;
BEGIN TRAN;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEAArdRExNwv0B5KyYOoNjHisWFfvoB5cpqMdTMVhPFr0OGEoxXjk8DyyXG5bt38MSQ==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer01@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEFvwlzVWTG55rEhs6f9Y1V/CUSfyPxQZUOZxo4702Zx66qs9x5ewYmj4BrCt0VXQUw==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer02@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEKiKZMv27hggaSIxFkWMolmssnNIS2sTQ8Jlezcoa1LyxxTElZNJ0Sk6B8SJnM5upg==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer03@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAENs48V1Za4+wqchkob+kFJooa9zSDR1OB8ArbNnpTlQjDM0CaRwiU75CjyLDtLYq0Q==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer04@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEL7uBmWVbY7GvUM8F0BJmHWeSu7WDME12ID2HEvEvDGYEJHs+/xINZgBcoE0y9TZww==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer05@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEEcdwGfh1n+/fDTuoUBO79o4JOA4rCo35DdYYmC9CYlD57P1QUD0DXpEN3c2YHYE6A==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer06@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAELVZDrelXfy+4B01Yt4aB4lMKUtbF82Kel0C4SxkWu+VZvLGnNxlIsaiczPRgRQXcw==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer07@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEBmkgb6XpMgUUDBxmHDXlYSQ5zfD+V6YGm7FPS35zsFc6m9yfxySlURZ5jMIXoo0NA==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer08@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAELAvv0Q696Lv5BDk/ZhAv4mk6wxjUvJ2XSwS1ilUngISNh1OQ8VATaO01+xPmvxoHA==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer09@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAECAnAP1Ha/yw5vL/cP5/jfFwuN8Q7XaHMJMkXmCcSpVDWxHQBdf4lzFFu/0hUyJ7Sw==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer10@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEFWZzjH20h3fEj+h2PMgg43VDvxHTgPEH4tzNYW3gmovnmpopJO6wofW3KCncMTreQ==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer11@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAELaPtEXia2Aig8h/K4SE6gpgbFymJ9U4ql0cHaaAQno4baYGTrrmR6x8+3pNPQMLew==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.buyer12@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEP+eQopqihxHz+UY2NFmHRDYKGpvp72TdRL2LqePDxv83l/cNzdneV6DbaJ2961RwA==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.seller1@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEKZhr+m09Gxlj9vzbgtaza8gqYtN/9ivmmAoFq2qSDZh30sj6IwlkugKpHQhYqLiDA==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.seller2@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEGhPGKD1x6tKEa8eeo9dM6ZjewLNXPugQCNr0Qo1znnplfc1cxc+/OjZySFc29KKsw==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.seller3@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEPQSvrkLwdYGFYtiaHVowPTQb+zeMjbSwaC1UccNClNXwt7lQtNTEHGDvKeXOfJFVA==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.seller4@example.test' AND passwordHash IS NULL;
UPDATE dbo.[User] SET passwordHash=N'AQAAAAIAAYagAAAAEN6pS+O9ApCcrcRVHtljIOZ9j43/NOVPmbqIwj49321pedUJ4nvXVGhsZggpX1bLGw==', updatedAt=SYSUTCDATETIME() WHERE email=N'seed.admin@example.test' AND passwordHash IS NULL;
COMMIT;
GO
