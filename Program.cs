using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Write("url: ");
        string url = Console.ReadLine();

        string date = DateTime.Now.ToString("dd-MM-yyyy");

        string operationalDir = Path.Combine(Directory.GetCurrentDirectory(), "operational");

        if (!Directory.Exists(operationalDir))
        {
            Directory.CreateDirectory(operationalDir);
        }

        string outputDir = Path.Combine(operationalDir, "output");

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        string sanitizedUrl = url.Replace("://", "_").Replace("/", "_");
        string txtFilePath = Path.Combine(outputDir, $"{DateTime.Now:HH-mm} {date} {sanitizedUrl} valid.txt");
        string jsonFilePath = Path.Combine(outputDir, $"{DateTime.Now:HH-mm} {date} {sanitizedUrl} valid.json");

        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/537.36 (KHTML, like Gecko) Version/14.0 Mobile/15E148 Safari/537.36");

                    string validatorUrl = $"https://validator.w3.org/nu/?doc={Uri.EscapeDataString(url)}&out=json";
                    HttpResponseMessage response = await client.GetAsync(validatorUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();

                        JObject json = JObject.Parse(responseContent);

                        string formattedJson = JsonConvert.SerializeObject(json, Formatting.Indented);
                        await File.WriteAllTextAsync(jsonFilePath, formattedJson);

                        var messages = json["messages"];
                        if (messages.HasValues)
                        {
                            Console.WriteLine("it's done");

                            using (StreamWriter writer = new StreamWriter(txtFilePath, false))
                            {
                                foreach (var message in messages)
                                {
                                    string type = message["type"]?.ToString();
                                    string text = message["message"]?.ToString();
                                    string line = message["lastLine"]?.ToString();
                                    string column = message["lastColumn"]?.ToString();
                                    string snippet = message["extract"]?.ToString();

                                    writer.WriteLine($"Type: {type}");
                                    writer.WriteLine($"Message: {text}");
                                    writer.WriteLine($"From line {line}, column {column}:");
                                    writer.WriteLine("Snippet:");
                                    writer.WriteLine(snippet);
                                    writer.WriteLine(new string('-', 50));
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("it's perfect.");
                        }
                    }
                    else
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"failed. status: {response.StatusCode}");
                        Console.WriteLine($"responsed with: {responseContent}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"err: {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine("???");
        }
    }
}
