# Reactive File Updater
The Reactive File Updater is a tool that runs as a Windows service in the background updating the files that you tell it to monitor. As soon as a monitored file is changed, it scans through the file for matches and runs a replacement on those found.

## Installation
Place `ReactiveFileUpdater.exe` in a folder of your choice and run it once (from a command prompt or Start Menu > Run) with parameters `-install -start` to install it as a Windows service and start it at the same time. It will be configured to start automatically at Windows startup. Create a settings file according to the [Configuration](#configuration) section.

## Uninstallation
Run `ReactiveFileUpdater.exe` (from a command prompt or Start Menu > Run) with parameters `-stop -uninstall` to stop it and uninstall it as a Windows service.

## Logs
(to be documented)

## Configuration
Place a `settings.json` file in `C:\ProgramData\ReactiveFileUpdater` with content according to the following format:
```json
{
   "FileUpdates": [
      {
         "FilePath": "<TARGET-FILE>",
         "SearchPattern": "<REGEX-PATTERN>",
         "ReplacePattern": "<REGEX-REPLACEMENT>"
      }
   ],
   "PollFrequency": "<POLL-TIMESPAN>"
}
```
Field|Description
-----|-----------
`TARGET-FILE`|The path to a file to monitor. Backslashes needs to be escaped, i.e. written as `\\`.
`REGEX-PATTERN`|A valid regular expression pattern used to find matches in the file. See https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference for a reference.
`REGEX-REPLACEMENT`|A valid regular expression replacement. See https://docs.microsoft.com/en-us/dotnet/standard/base-types/substitutions-in-regular-expressions for a reference.
`POLL-TIMESPAN`|The time between polls, formatted as a timespan `[d.]hh:mm:ss[.fffffff]`.
