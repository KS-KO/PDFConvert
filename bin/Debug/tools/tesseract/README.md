Portable Tesseract runtime folder.

Place the following files here to use `Tesseract` without installing it system-wide:

- `tesseract.exe`
- required DLL files shipped with your Tesseract build
- `tessdata\eng.traineddata`
- `tessdata\kor.traineddata` if Korean OCR is needed

At build time, everything under this folder is copied to:

- `bin\Debug\tools\tesseract\`
- `bin\Release\tools\tesseract\`

The application looks for `tools\tesseract\tesseract.exe` first, so a bundled runtime works without PATH or installer setup.
