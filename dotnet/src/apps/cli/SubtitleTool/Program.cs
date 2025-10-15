using System.CommandLine;
using System.Threading.Tasks;
using SubtitleTool;
using SubtitleTool.Commands;

namespace SubtitleTool;

public class Program
{
                    public static async Task<int> Main(string[] args)
                    {
                        var rootCommand = new RootCommand("A unified CLI tool for subtitle processing.");
            
                        rootCommand.AddCommand(new ConvertCommand());
                        rootCommand.AddCommand(new TranslateCommand());
                        rootCommand.AddCommand(new GeminiTestCommand());
            
                        return await rootCommand.InvokeAsync(args);
                    }}