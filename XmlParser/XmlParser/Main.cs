using System.Diagnostics;

namespace XmlParser
{
    abstract class Program
    {
        static async Task Main(string[] args)
        {
            var filePath = "../../../../Orders.xml";
            var outputPath = "../../../../Output.txt";

            string xmlString = await File.ReadAllTextAsync(filePath);
            
            var parser = new ForwardOnlyParser();
            
            parser.SetOutputPath(outputPath);

            var stopwatch = Stopwatch.StartNew();

            parser.Parse(xmlString, 100, true);

            stopwatch.Stop();
            Console.WriteLine($"Parsing took {stopwatch.ElapsedMilliseconds} ms.");
        }
    }
}