# -*- coding: utf-8 -*-
"""
    Pygments
    ~~~~~~~~

    Pygments is a syntax highlighting package written in Python.

    It is a generic syntax highlighter for general use in all kinds of software
    such as forum systems, wikis or other applications that need to prettify
    source code. Highlights are:

    * a wide range of common languages and markup formats is supported
    * special attention is paid to details, increasing quality by a fair amount
    * support for new languages and formats are added easily
    * a number of output formats, presently HTML, LaTeX, RTF, SVG and ANSI sequences
    * it is usable as a command-line tool and as a library
    * ... and it highlights even Brainfuck!

    The `Pygments tip`_ is installable with ``easy_install Pygments==dev``.

    .. _Pygments tip:
       http://dev.pocoo.org/hg/pygments-main/archive/tip.tar.gz#egg=Pygments-dev

    :copyright: Copyright 2006-2009 by the Pygments team, see AUTHORS.
    :license: BSD, see LICENSE for details.
"""

__version__ = '1.1'
__docformat__ = 'restructuredtext'

__all__ = ['lex', 'format', 'highlight']


import sys, os

from pygments.util import StringIO, BytesIO


def lex(code, lexer):
    """
    Lex ``code`` with ``lexer`` and return an iterable of tokens.
    """
    try:
        return lexer.get_tokens(code)
    except TypeError, err:
        if isinstance(err.args[0], str) and \
           'unbound method get_tokens' in err.args[0]:
            raise TypeError('lex() argument must be a lexer instance, '
                            'not a class')
        raise


def format(tokens, formatter, outfile=None):
    """
    Format a tokenlist ``tokens`` with the formatter ``formatter``.

    If ``outfile`` is given and a valid file object (an object
    with a ``write`` method), the result will be written to it, otherwise
    it is returned as a string.
    """
    try:
        if not outfile:
            #print formatter, 'using', formatter.encoding
            realoutfile = formatter.encoding and BytesIO() or StringIO()
            formatter.format(tokens, realoutfile)
            return realoutfile.getvalue()
        else:
            formatter.format(tokens, outfile)
    except TypeError, err:
        if isinstance(err.args[0], str) and \
           'unbound method format' in err.args[0]:
            raise TypeError('format() argument must be a formatter instance, '
                            'not a class')
        raise


def highlight(code, lexer, formatter, outfile=None):
    """
    Lex ``code`` with ``lexer`` and format it with the formatter ``formatter``.

    If ``outfile`` is given and a valid file object (an object
    with a ``write`` method), the result will be written to it, otherwise
    it is returned as a string.
    """
    return format(lex(code, lexer), formatter, outfile)


if __name__ == '__main__':
    from pygments.cmdline import main
    sys.exit(main(sys.argv))
