import re
from pygments.filter import Filter
from pygments.token import Comment, Token


class UniversalCommentDelimiterSplit(Filter):

    def __init__(self, **options):
        super().__init__(**options)  # ✅ Important to avoid NotImplementedError

    COMMENT_PATTERNS = [
        (re.compile(r'^(/\*)(.*?)(\*/)$', re.DOTALL), '/*', '*/'),  # C-style
        (re.compile(r'^(<!--)(.*?)(-->)$', re.DOTALL), '<!--', '-->'),  # HTML
        (re.compile(r'^(--)(.*)', re.DOTALL), '--', ''),  # SQL/Lua single-line
        (re.compile(r'^(//)(.*)', re.DOTALL), '//', ''),  # C++/Java single-line
        (re.compile(r'^(#)(.*)', re.DOTALL), '#', ''),  # Python/Bash/YAML
        (re.compile(r"^(''')(.*)(''')$", re.DOTALL), "'''", "'''"),  # Python triple
        (re.compile(r'^(--\[\[)(.*)(\]\])$', re.DOTALL), '--[[', ']]'),  # Lua multi-line
    ]

    def filter(self, lexer, stream):
        for ttype, value in stream:
            if ttype in Comment:
                for pattern, start, end in self.COMMENT_PATTERNS:
                    m = pattern.match(value)
                    if m:
                        if start:
                            yield (Token.Comment.Special, m.group(1))
                        if m.lastindex >= 2 and m.group(2):
                            yield (ttype, m.group(2))
                        if m.lastindex == 3 and end:
                            yield (Token.Comment.Special, m.group(3))
                        break
                else:
                    yield (ttype, value)
            else:
                yield (ttype, value)
