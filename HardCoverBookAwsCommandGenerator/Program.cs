using HardCoverBookAwsCommandGenerator;

string environment = "";
string lambdaArn = "";
string pdfGenBcuketName = "";

Console.WriteLine($"** Generate AWS lambda invoke command ** {Environment.NewLine}");

while (String.IsNullOrEmpty(environment))
{
    Console.WriteLine($"Choose envirionment: {Environment.NewLine} 1. DEV {Environment.NewLine} 2. STAGE {Environment.NewLine} 3. PROD");
    string choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            environment = "DEV";
            pdfGenBcuketName = $"whcc-production-pdfgen-claimcheck-{environment.ToLower()}-ue2";
            lambdaArn = "arn:aws:lambda:us-east-1:845720824676:function:PDFGen";
            break;
        case "2":
            environment = "STAGE";
            pdfGenBcuketName = $"whcc-production-pdfgen-claimcheck-{environment.ToLower()}-ue2";
            lambdaArn = "arn:aws:lambda:us-east-1:845720824676:function:PDFGen";
            break;
        case "3":
            environment = "PROD";
            pdfGenBcuketName = $"whcc-production-pdfgen-claimcheck-{environment.ToLower()}-ue2";
            lambdaArn = "arn:aws:lambda:us-east-1:845720824676:function:PDFGen";
            break;
        default:
            Console.WriteLine("Invalid choice!");
            break;
    }
}

Console.WriteLine($"Generating the commands for environment: {environment}");

string commandSavePath = @"C:/Temp/";
string s3ObjectSavePath = @"C:/Temp/s3ObjectSave/";
string sqlQuerySavePath = @"C:/Temp/SQL/";

// Generate S3 get object command
Console.WriteLine($"Enter input JSON: ");
string jsonString = Console.ReadLine();

PdfGenEvent inputJsonObject = PdfGenEvent.GetJsonObject(jsonString);
string s3ObjectKey = PdfGenEvent.GetObjectKeyNameFromAssetPath(inputJsonObject.OrderAssetPath);
string[] splitObjectKey = s3ObjectKey.Split('/');

string s3GetCommand = $"aws s3api get-object --bucket {pdfGenBcuketName} --key {s3ObjectKey} --profile whcc-platform-{environment.ToLower()} {s3ObjectSavePath}{splitObjectKey[splitObjectKey.Length-1]}";
WriteCommand(commandSavePath, s3GetCommand, $"{inputJsonObject.OrderUID}_s3GetCommand.txt");

string outfileName = $"Outfile.json";

// Gemerate Lambda invoke command.
string lambdaInvokeCommand = @$"aws lambda invoke --function-name {lambdaArn} --qualifier {environment.ToUpper()} --profile pdfgen-invoke-only --cli-binary-format raw-in-base64-out --cli-read-timeout 1200 --payload file://{s3ObjectSavePath}{splitObjectKey[splitObjectKey.Length - 1]} {commandSavePath}{outfileName}";
WriteCommand(commandSavePath, lambdaInvokeCommand, $"{inputJsonObject.OrderUID}_lambdaInvokeCommand.txt");

// Generate SQL query
string query = $@"USE OrderStaging 
                    BEGIN TRY
	                    BEGIN TRANSACTION 

	                    Select * from OrderStaging.dbo.Orders Where OrderUID = {inputJsonObject.OrderUID}
	                    Select * from OrderItemAsset where OrderItemAssetUID = {inputJsonObject.ItemAssetUID}

	                    Update OrderItemAsset set AssetPath = '{inputJsonObject.OrderAssetPath}' where OrderItemAssetUID = {inputJsonObject.ItemAssetUID}

	                    Update OrderStaging.dbo.Orders set OrderStatusID = '5', ProcessedDt = null Where OrderUID = {inputJsonObject.OrderUID}

	                    Select * from  OrderStaging.dbo.Orders Where OrderUID = {inputJsonObject.OrderUID}
	                    Select * from OrderItemAsset where OrderItemAssetUID = {inputJsonObject.ItemAssetUID}

	                    COMMIT TRANSACTION
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0
                            ROLLBACK TRAN

                            DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE()
                            DECLARE @ErrorSeverity INT = ERROR_SEVERITY()
                            DECLARE @ErrorState INT = ERROR_STATE()
 
                        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
                    END CATCH";

WriteCommand(sqlQuerySavePath, query, $"{inputJsonObject.OrderUID}.sql");

static void WriteCommand(string path, string command, string fileName)
{
    if (!String.IsNullOrEmpty(command) && !String.IsNullOrEmpty(path))
        File.WriteAllText($@"{path}\{fileName}", command);
}

