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

Console.WriteLine($"** Input Json ** {Environment.NewLine}");
string jsonData = Console.ReadLine();

// Generate S3 get object command
PdfGenEvent inputJsonObject = PdfGenEvent.GetJsonObject(jsonData);

string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
//string commandSavePath = @$"{userDirectory}/Temp/{inputJsonObject.OrderUID}";
string commandSavePath = Path.Combine(new string[] { userDirectory, "Temp", inputJsonObject.OrderUID.ToString() });
string commandFile = Path.Combine(new string[] { commandSavePath, $"{inputJsonObject.OrderUID}-commands.txt" });

// Create a unique directory
Directory.CreateDirectory(commandSavePath);
DeleteFileIfExists(commandFile);

string s3ObjectKey = PdfGenEvent.GetObjectKeyNameFromAssetPath(inputJsonObject.OrderAssetPath);
string[] splitObjectKey = s3ObjectKey.Split('/');

// Create the s3output file
string s3OutputFile = Path.Combine(new string[] { commandSavePath, splitObjectKey[splitObjectKey.Length - 1] });

AppendToFile(commandFile, "aws sso login --sso-session whcc-sso");
AppendToFile(commandFile, "https://admin.whcc.com/order-album-browser.php?sort=desc&acct=161333");

string s3GetCommand = $"aws s3api get-object --bucket {pdfGenBcuketName} --key {s3ObjectKey} --profile whcc-platform-{environment.ToLower()} {s3OutputFile.Replace(" ", @"\")}";
AppendToFile(commandFile, s3GetCommand,2);

string outfileName = $"pdfGenResult.json";

// Gemerate Lambda invoke command.
string lambdaInvokeCommand = @$"aws lambda invoke --function-name {lambdaArn} --qualifier {environment.ToUpper()} --profile whcc-dogbone-{environment.ToLower()} --cli-binary-format raw-in-base64-out --cli-read-timeout 1200 --payload file://{s3OutputFile.Replace(" ", @"\")} {Path.Combine(new string[] { commandSavePath.Replace(" ", @"\") , outfileName })}";
AppendToFile(commandFile, lambdaInvokeCommand,2);

// Generate SQL query
string query = $@"USE OrderStaging 
                    BEGIN TRY
	                    BEGIN TRANSACTION 

	                    Select * from OrderStaging.dbo.Orders Where OrderUID = {inputJsonObject.OrderUID}
	                    Select * from OrderItemAsset where OrderItemAssetUID = {inputJsonObject.ItemAssetUID}

	                    Update OrderItemAsset set AssetPath = '' where OrderItemAssetUID = {inputJsonObject.ItemAssetUID}

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

AppendToFile(commandFile, query,2);

static void DeleteFileIfExists(string filePath)
{
    if (File.Exists(filePath))
    {
        try
        {
            File.Delete(filePath);
            Console.WriteLine("File deleted successfully.");
        }
        catch (IOException e)
        {
            Console.WriteLine($"An error occurred while deleting the file: {e.Message}");
        }
    }
    else
    {
        Console.WriteLine("The file does not exist.");
    }
}


static void AppendToFile(string filePath, string data, int newLineCount = 0)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                for (int i = 0; i < newLineCount; i++)
                {
                    writer.WriteLine();
                }

                writer.WriteLine(data);
            }

            Console.WriteLine("Data appended successfully.");
        }
        catch (IOException e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }
    }
