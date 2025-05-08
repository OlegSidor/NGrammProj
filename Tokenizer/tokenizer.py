import sys
import io
import json
import os
from pygments import lex
from pygments.lexers import guess_lexer_for_filename, get_lexer_by_name
from pygments.token import Token
import re
from universal_comment_filter import UniversalCommentDelimiterSplit

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')


def tokenize_code(code, file_path):
    try:
        lexer = guess_lexer_for_filename(file_path, code)
    except Exception:
        lexer = get_lexer_by_name("text")

    lexer.add_filter(UniversalCommentDelimiterSplit())

    code = re.sub(r'\\u[0-9A-Fa-f]{4}', lambda m: chr(int(m.group(0)[2:], 16)), code)
    tokens = lex(code, lexer)
    token_list = []

    for token_type, token_value in tokens:
        if token_type in Token.Text:
            continue

        token_list.append({
            "type": str(token_type),
            "value": token_value
        })

    return token_list


def main():
    if len(sys.argv) < 2:
        print(json.dumps({"error": "No file path provided"}, ensure_ascii=False))
        sys.exit(1)

    file_path = sys.argv[1]

    if not os.path.exists(file_path):
        print(json.dumps({"error": f"File not found: {file_path}"}, ensure_ascii=False))
        sys.exit(1)

    try:
        with open(file_path, "r", encoding="utf-8") as f:
            code = f.read()
    except Exception as e:
        print(json.dumps({"error": f"Failed to read file: {str(e)}"}, ensure_ascii=False))
        sys.exit(1)

    tokens = tokenize_code(code, file_path)

    print(json.dumps(tokens, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
