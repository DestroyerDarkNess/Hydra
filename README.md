<div align="center">
 
[![Create Go App][repo_logo_img]][repo_url]
 
# HydraEngine

[![.NET Framework][dotnet_version_img]][dotnet_dev_url]
[![.NET Framework][dotnet_core_version_img]][dotnet_dev_url]
[![License][repo_license_img]][repo_license_url]

Hydra is a powerful tool for protecting .NET assemblies with various obfuscation and anti-debugging techniques. Focus on writing your code and let Hydra handle the protection!
<br><br>

⚠️ I invite you to the community, where you can chat and suggest your ideas for this project. 💌 
 
[![Discord Banner 2](https://discord.com/api/guilds/1327640073348317235/widget.png?style=banner2)](https://discord.gg/kmU8d3WDgm)
</div>



## ⚡️ Quick start

First, ensure you have **[.NET Framework 4.8](https://go.microsoft.com/fwlink/?linkid=2088631)** or higher installed.

[Download Hydra](https://github.com/DestroyerDarkNess/Hydra/releases/) and have fun.
 
That's all you need to know to start! 🎉

## 🔨 Building from Source

If you want to compile Hydra from source code, follow these steps:

### Prerequisites
- **Visual Studio 2019** or later (with .NET Framework 4.8 support)
- **.NET Framework 4.8** or higher
- **Git** for cloning the repository

### Step-by-Step Build Process
 
📹 **[Watch the complete build process video](https://github.com/DestroyerDarkNess/Hydra/HydraBuild.mp4)** - Step by step compilation guide

### Troubleshooting
- Ensure you have the correct .NET Framework version installed
- Make sure all NuGet packages are properly restored
- Check that you have sufficient permissions to build the project

## ⚙️ Features & Options

Listed below are all the features of Hydra:
 
| Feature| Description                                              | .Net Framework   | .Net Core | 
| ------ | -------------------------------------------------------- | ---------------- | --------- |
| 🔄 `Renamer`   | Obfuscates the original assembly by renaming Methods, Properties, Events, Classes, Fields, Namespaces and even the Module of the assembly. Supports exclusion via `HydraNoObfuscate` attribute.             | ✅ | ✅  |  

🚧 Under Construction. 🚧

### 🔄 Renamer - HydraNoObfuscate Attribute

The Renamer feature now supports excluding specific classes and methods from obfuscation using the `HydraNoObfuscate` attribute.

#### Usage Example:

```csharp
// 1. Define the attribute in your code
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event)]
public class HydraNoObfuscateAttribute : Attribute
{
}

// 2. Apply to classes you want to exclude completely
[HydraNoObfuscate]
public class ImportantAPIClass
{
    // All members of this class will be excluded from renaming
    public void PublicMethod() { }
    public string PublicProperty { get; set; }
    public string PublicField;
}

// 3. Apply to specific members you want to exclude
public class MyClass
{
    [HydraNoObfuscate]
    public void DoNotRenameThisMethod() 
    {
        // This method will keep its original name
    }
    
    public void ThisMethodWillBeRenamed() 
    {
        // This method will be renamed normally
    }
    
    [HydraNoObfuscate]
    public string ImportantProperty { get; set; }
    
    [HydraNoObfuscate]
    public string criticalField;
    
    [HydraNoObfuscate]
    public event EventHandler ImportantEvent;
}
```

#### Key Features:
- **Hierarchical Protection**: If a class has `[HydraNoObfuscate]`, all its members are automatically protected
- **Granular Control**: Apply the attribute to specific methods, properties, fields, or events
- **Flexible Naming**: The attribute works with both `HydraNoObfuscate` and `HydraNoObfuscateAttribute` names
- **Zero Configuration**: Just add the attribute definition to your code and start using it
 
## Documentation

- 📖 [Complete Command Line Guide](Hydra/COMMANDLINE_README.md)
- 📝 [Preset System Documentation](Hydra/PRESETS_README.md)
- 📁 [Example Scripts](Hydra/Examples/)

## ⭐️ Project assistance

If you want to say **thank you** or/and support active development of `Hydra`:

- Add a [GitHub Star][repo_url] to the project.

## ❗️ Support the author

You can support the author on Binance or Paypal `s4lsalsoft@gmail.com`. ❤️

## 🏆 A win-win cooperation

And now, I invite you to participate in this project!  

- [Issues][repo_issues_url]: ask questions and submit your features.
- [Pull requests][repo_pull_request_url]: send your improvements to the current.
- Join our [Discord community](https://discord.gg/kmU8d3WDgm) to discuss ideas and get help.

Together, we can make this project **better** every day! 😘

## ⚠️ License

[`Hydra`][repo_url] is free and open-source software licensed under the [MIT License][repo_license_url].

<!-- .NET -->

[dotnet_version_img]: https://img.shields.io/badge/.NET_Framework-violet?style=for-the-badge&logo=dotnet
[dotnet_core_version_img]: https://img.shields.io/badge/.NET_Core-blue?style=for-the-badge&logo=dotnet
[dotnet_dev_url]: https://dotnet.microsoft.com/

<!-- Repository -->
[repo_logo_url]: https://github.com/DestroyerDarkNess/Hydra
[repo_logo_img]: https://github.com/user-attachments/assets/2b36e5d4-0122-4691-9fda-2dfb0acfb7cc
[repo_url]: https://github.com/DestroyerDarkNess/Hydra
[repo_license_url]: https://github.com/DestroyerDarkNess/Hydra/blob/main/LICENSE
[repo_license_img]: https://img.shields.io/badge/license-MIT-red?style=for-the-badge&logo=none
[repo_issues_url]: https://github.com/DestroyerDarkNess/Hydra/issues
[repo_pull_request_url]: https://github.com/DestroyerDarkNess/Hydra/pulls
[repo_discussions_url]: https://github.com/DestroyerDarkNess/Hydra/discussions
[repo_wiki_url]: https://github.com/DestroyerDarkNess/Hydra/wiki

<!-- Author -->

[boosty_url]: https://github.com/DestroyerDarkNess

<!-- Readme links -->

[dev_to_url]: https://dev.to/

