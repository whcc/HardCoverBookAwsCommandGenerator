string filePath = @"C:\Temp\";
string payloadFileName = "Monolith.json";
string outfileName = "Outfile.txt";
string command = "";
string environment = "";

Console.WriteLine($"** Generate AWS lambda invoke command ** {Environment.NewLine}");

while (String.IsNullOrEmpty(environment))
{
    Console.WriteLine($"Choose envirionment: {Environment.NewLine} 1. DEV {Environment.NewLine} 2. STAGE {Environment.NewLine} 3. PROD");
    string choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            environment = "DEV";
            break;
        case "2":
            environment = "STAGE";
            break;
        case "3":
            environment = "PROD";
            break;
        default:
            Console.WriteLine("Invalid choice!");
            break;
    }
}

Console.WriteLine($"Running the command in environment: {environment}");
command = @$"aws lambda invoke --function-name PDFGen:{environment} --region us-east-1 --profile whcc-dgbn --payload {filePath}{payloadFileName} {filePath}{outfileName}";
WriteCommand(filePath, command, "GeneratedCommand.txt");

static void WriteCommand(string path, string command, string fileName)
{
    if (!String.IsNullOrEmpty(command) && !String.IsNullOrEmpty(path))
        File.WriteAllText($@"{path}\{fileName}", command);
}