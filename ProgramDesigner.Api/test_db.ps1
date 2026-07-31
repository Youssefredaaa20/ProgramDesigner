
$connectionString = "Server=DESKTOP-QEDOLN5\SQLEXPRESS01;Database=ProgramDesigner;Trusted_Connection=True;TrustServerCertificate=True;"
$query = "SELECT Id, ProgramId, ParentId, Name FROM ProgramNodes"
Invoke-Sqlcmd -ServerInstance "DESKTOP-QEDOLN5\SQLEXPRESS01" -Database "ProgramDesigner" -Query $query

