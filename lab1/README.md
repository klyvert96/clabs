# Book Color Palette

Console application for the assignment: it reads a Russian TXT book, finds color words and writes a PNG palette in the order the colors occur.

## Run

When you build the project, it automatically processes `Podarok.txt` and creates or updates `palette.png` in the project folder.
Launching the project without arguments also prints the color statistics in the console.

To process a different file or use other layout options manually:

```powershell
dotnet run --project .\BookColorPalette.csproj -- .\Aeroport.txt .\palette.png --layout grid --size 36 --columns 20
```

For a one-row palette, use `--layout line`.

The program accepts UTF-8 (with or without BOM) and automatically falls back to Windows-1251 when the file is not valid UTF-8. It prints frequency statistics to the console.

## Example

For `Красное солнце садилось за синие горы. Белый снег покрывал землю.` the result contains red, blue and white squares, and the console reports each color once.
