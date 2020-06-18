# PHPUnit for Peachpie

This repository contains the following NuGet packages:
- `Peachpied.PhpUnit` - [PHPUnit](https://phpunit.de) compiled by Peachpie to a .NET Standard assembly, to be referenced by projects containing tests.
- `dotnet-phpunit` - A command line tool `dotnet phpunit` to enable running [PHPUnit](https://phpunit.de) on Peachpie projects.

## How to run the samples

At first, compile the solution by running this command in the repository root:

```
dotnet build
```

This will create the NuGet packages in the `nugs` folder and compile `samples/Lib`.
To test `dotnet-phpunit` on `samples/Lib` (which references `Peachpied.PhpUnit` as a project), just run it using the default launch settings, as they already contain all the necessary arguments.

The second sample, `LibNuget`, references `Peachpied.PhpUnit` as a NuGet package.
To test it, run the following commands in the directory `samples/LibNuget`:

```
dotnet tool install --global dotnet-phpunit
dotnet build
dotnet phpunit obj/Debug/netstandard2.0/LibNuget.dll --bootstrap src/Email.php tests/EmailTest.php
```

It should run PHPUnit with the output similar to this one:
```
Runner of PHPUnit (© Sebastian Bergmann) on PHP assemblies compiled by Peachpie

Opening assembly "C:\repos\peachpie-phpunit\samples\LibNuget\obj\Debug\netstandard2.0\LibNuget.dll"...
Assembly loaded

PHPUnit 9.2.3 by Sebastian Bergmann and contributors.

...                                                                 3 / 3 (100%)

Time: 00:00.696, Memory: 39.29 KB

OK (3 tests, 3 assertions)
```

If you wish to remove `dotnet-phpunit` after using it this way, run:

```
dotnet tool uninstall --global dotnet-phpunit
```
