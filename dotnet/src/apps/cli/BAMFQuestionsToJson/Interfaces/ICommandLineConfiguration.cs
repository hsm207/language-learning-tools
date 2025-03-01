using System.CommandLine;

namespace LanguageLearningTools.BAMFQuestionsToJson.Interfaces;

/// <summary>
/// Interface for configuring command line options and commands.
/// </summary>
internal interface ICommandLineConfiguration
{
    /// <summary>
    /// Creates and configures a root command with all necessary options.
    /// </summary>
    /// <returns>A configured RootCommand instance.</returns>
    RootCommand BuildRootCommand();

    /// <summary>
    /// Registers the command handler with the provided service factory.
    /// </summary>
    /// <param name="rootCommand">The command to register the handler with.</param>
    /// <param name="serviceFactory">The service factory to use for dependency resolution.</param>
    void RegisterCommandHandler(RootCommand rootCommand, IServiceFactory serviceFactory);
}