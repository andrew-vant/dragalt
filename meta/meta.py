#!/usr/bin/python3

""" Generate ckan version metadata file """

import sys
import json
import logging
from logging import getLogger, WARNING, DEBUG
from argparse import ArgumentParser, FileType
from os.path import dirname, realpath
from subprocess import run, PIPE, CalledProcessError
from functools import partial
from enum import IntEnum

try:
    import yaml
    import jinja2
    from semantic_version import Version
except ImportError as ex:
    print(f"missing dependency: {ex.name}", file=sys.stderr)
    sys.exit(1)


METADIR = dirname(realpath(__file__))  # This directory
MD_NATIVE = METADIR + '/meta.yaml'     # Default native metadata file
MD_CKAN = "version.yaml.jinja"         # Default ckan metadata template
LOGFMT = "%(levelname)s:  %(message)s"

class err(IntEnum):
    ok = 0
    norelease = 1
    cli = 2
    other = 3

def cliargs(argv=None):
    """ Get command line arguments """
    if argv is None:
        argv = sys.argv[1:]
    parser = ArgumentParser(description=__doc__)
    addarg = parser.add_argument
    addflag = partial(parser.add_argument, action='store_true')
    addarg('-i', '--infile', type=FileType('r'), default=MD_NATIVE,
           help='input filename')
    addarg('-o', '--outfile', type=FileType('w'), default=sys.stdout,
           help='output filename')
    addflag('-v', '--verbose', help='verbose output')
    addflag('-I', '--interim', help='print interim data')
    return parser.parse_args(argv)


def initlog(level):
    """ Set up logging """
    logging.basicConfig(format=LOGFMT, level=level)
    return getLogger()


def main(argv=None):
    """ Generate ckan version metadata file """

    args = cliargs(argv)
    log = initlog(DEBUG if args.verbose else WARNING)

    # Collect static metadata
    static = yaml.safe_load(args.infile)

    # Collect git version tag
    cmd = 'git describe --exact-match --tags --dirty'
    try:
        result = run(cmd.split(), check=True, capture_output=True)
    except CalledProcessError as ex:
        log.error("git error: %s", ex.stderr.decode().strip())
        log.error("no tag for current commit?")
        sys.exit(err.other)
    vstr = result.stdout.decode().strip().lstrip('v')
    dirty = vstr.endswith('-dirty')
    if dirty:
        vstr = vstr[:-len('-dirty')]
    try:
        version = Version(vstr)
    except ValueError as ex:
        log.error(ex)
        sys.exit(err.other)
    if dirty:
        version.build = ('dirty',)
    log.info("version found: %s", version)
    log.debug(vars(version))

    # Render template
    jenv = jinja2.Environment(
        loader=jinja2.FileSystemLoader(METADIR),
        keep_trailing_newline=True
        )
    tpl = jenv.get_template(MD_CKAN)
    rendered = tpl.render(version=version, **static)
    if args.interim:
        print(f'---\n{rendered}---')
    json.dump(yaml.safe_load(rendered), args.outfile, indent=2)
    print()  # because json.dump omits the final newline

    if version.prerelease:
        log.warning("DO NOT RELEASE: %s is not a release version", version)
        sys.exit(err.norelease)
    if dirty:
        log.warning("DO NOT RELEASE: working directory is dirty")
        sys.exit(err.norelease)



if __name__ == '__main__':
    main()
