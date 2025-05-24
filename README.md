# NGramm

Щоб збілдити проект, потрібно компілювати tokenizer.py в exe
і перенести його в папку NGramm\CodeTokenizatorTool\tokenizer.exe

Або можна завантажити його із Releases в GitHub

Для запуску скомпільованого проекту, python не потрібен.

# Tokenizer

Create exe:
```
pyinstaller --onefile --console tokenizer.py
```
або 

```
python -m PyInstaller --onefile --console tokenizer.py
```

PYTHON ПОВИНЕН БУТИ ВЕРСІЇ 3.6-3.6.8!!!


Встановлення бібліотек

```
pip install -r requirements.txt
```
або
```
python -m pip install -r requirements.txt
```