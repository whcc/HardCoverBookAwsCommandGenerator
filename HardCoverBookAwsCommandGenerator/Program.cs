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
string inputJsonFilePath = @"C:/Temp/InputJson/InputJson.Json";
string s3ObjectSavePath = @"C:/Temp/s3ObjectSave/";

// S3 get object command
PdfGenEvent inputJsonObject = PdfGenEvent.GetJsonObject(inputJsonFilePath);
string s3ObjectKey = PdfGenEvent.GetJsonFileNameFromAssetPath(inputJsonObject.OrderAssetPath);

string s3GetCommand = $"aws s3api get-object --bucket {pdfGenBcuketName} --key {s3ObjectKey} --profile whcc-platform-{environment.ToLower()} {s3ObjectSavePath}{s3ObjectKey}"; ;
WriteCommand(commandSavePath, s3GetCommand, $"{inputJsonObject.OrderUID}_s3GetCommand.txt");

string outfileName = $"Outfile.json";

// Lambda invoke command.
string lambdaInvokeCommand = @$"aws lambda invoke --function-name {lambdaArn} --qualifier {environment.ToUpper()} --profile whcc-dogbone-{environment.ToLower()} --cli-binary-format raw-in-base64-out --cli-read-timeout 1200 --payload file://{s3ObjectSavePath}{s3ObjectKey} {commandSavePath}{outfileName}";
WriteCommand(commandSavePath, lambdaInvokeCommand, $"{inputJsonObject.OrderUID}_lambdaInvokeCommand.txt");

static void WriteCommand(string path, string command, string fileName)
{
    if (!String.IsNullOrEmpty(command) && !String.IsNullOrEmpty(path))
        File.WriteAllText($@"{path}\{fileName}", command);
}

// Just one outfile.