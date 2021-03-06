# GitHistoryAnalyzer
Small C# tool that parses and analyzes git history log as generated by TortoiseGit
 (Copy & Paste)

## Usage
GitHistoryAnalyzer Operation GITLOGFILE OUTPUTCSVFILE [AUTHORLIST]

### Possible Operations
Operation | Description
--------- | -----------
FindNewcomers     | Find all contributors to a git repository by the date of their first commit
CountContributors | Count the all time number of commits for each contributor

### Options
Option | Description
------ | -----------
GITLOGFILE      | Path to a git log file as generated from TortoiseGit per Copy & Paste
OUTPUTCSVFILE   | Path to which results will be written as CSV; overwrites existing files
AUTHORLIST      | Path to a file with authors; one author per row, semicolons separate synonyms; Will be created with all heuristically deanonymized author names if it does not exist already.

# License
GitHistoryAnalyzer is released under the [X11 License](https://github.com/paluno/GitHistoryAnalyzer/blob/master/LICENSE.md).
