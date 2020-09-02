# PHPUnit for Peachpie

This repository contains the following NuGet packages:
- `Peachpied.PhpUnit` - [PHPUnit](https://phpunit.de) compiled by Peachpie to a .NET Standard assembly, to be referenced by projects containing tests.
- `Peachpied.PhpUnit.TestAdapter` - A custom test adapter for VSTest enabling to run PHPUnit tests from `dotnet test` and Test Explorer in Microsoft Visual Studio.
- `dotnet-phpunit` - A command line tool `dotnet phpunit` to enable running [PHPUnit](https://phpunit.de) on Peachpie projects.

They can be tested on the following two samples:
- `Lib` - A simple .NET Core library with tests and PHPUnit configuration stored in `phpunit.xml`. It references both `dotnet-phpunit` and `Peachpied.PhpUnit.TestAdapter` as projects.
- `LibNuget` - A similar library but referencing both the projects as NuGet package.

## How to run the samples

At first, compile the solution by running this command in the repository root:

```
dotnet build
```

This will create the NuGet packages in the `nugs` folder and compile `samples/Lib`.

Notice that both `samples/Lib` and `samples/LibNuget` are .NET Core libraries.
Although `dotnet-phpunit` works on .NET Standard libraries as well, the test adapter (for the purposes of `dotnet test` and VS Test Explorer) does not.

### Test Adapter for VSTest

To run them from the command line, just navigate to either `samples/Lib` or `samples/LibNuget` and run:

```
dotnet test
```

The output should be similar to this one (possibly preceeded by the build log):

```
Test run for C:\iolevel\peachpie-phpunit\samples\Lib\bin\Debug\netcoreapp3.1\Lib.dll(.NETCoreApp,Version=v3.1)
Microsoft (R) Test Execution Command Line Tool Version 16.6.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...

A total of 1 test files matched the specified pattern.

Test Run Successful.
Total tests: 3
     Passed: 3
 Total time: 4.2106 Seconds
```

You can also open either of these projects in Microsoft Visual Studio and use the Test Explorer window to inspect the existing tests and run them.

### `dotnet phpunit`

To test `dotnet-phpunit` on `samples/Lib` (which references `Peachpied.PhpUnit` as a project), just run it using the default launch settings.

The second sample, `LibNuget`, references `Peachpied.PhpUnit` as a NuGet package.
To test it, run the following commands in the directory `samples/LibNuget`:

```
dotnet tool install --global dotnet-phpunit
dotnet build
dotnet phpunit
```

Both variants should run PHPUnit with the output similar to this one:
```
Runner of PHPUnit (© Sebastian Bergmann) on PHP assemblies compiled by Peachpie

Building "C:\repos\peachpie-phpunit\samples\LibNuget\LibNuget.msbuildproj"...
Opening assembly "C:\repos\peachpie-phpunit\samples\LibNuget\obj\Debug\netstandard2.0\LibNuget.dll"...
Assembly loaded

PHPUnit 9.2.3 by Sebastian Bergmann and contributors.

...                                                                 3 / 3 (100%)

Time: 00:00.696, Memory: 39.29 KB

OK (3 tests, 3 assertions)
```

To explicitly disable building of the project (as it's already built), pass the `--no-build` option to `dotnet phunit`, e.g.:

```
dotnet phpunit --no-build
```

If you wish to remove `dotnet-phpunit` from the set of installed tools, run:

```
dotnet tool uninstall --global dotnet-phpunit
```
