#!/bin/bash 
# Fill in original sources in the results from a FindClones run
# Special version for normalized source

# Revised 1.10.18

ulimit -s hard

# Find our installation
lib="${0%%/scripts/getnormsource.sh}"
if [ ! -d ${lib} ]
then
    echo "*** Error:  cannot find NiCad installation ${lib}"
    echo ""
    exit 99
fi

date

pcfile="$1"
if [ ! -f "$pcfile" ] 
then
    echo "Usage:  GetNormSource system-name_granularity.xml system-name_granularity-clones/system-name_granularity-clones-threshold.xml"
    echo "  (Output in system-name_granularity-clones/system-name_granularity-clones-threshold-normsource.xml)"
    echo "  e.g., GetNormSource systems/c/linux_functions.xml systems/c/linux_functions-clones/linux_functions-clones-0.2.xml"
    exit 99
fi

shift

# Check we have a system
clonesfile="$1"
if [ ! -f "$clonesfile" ] 
then
    echo "Usage:  GetNormSource system-name_granularity.xml system-name_granularity-clones/system-name_granularity-clones-threshold.xml"
    echo "  (Output in system-name_granularity-clones/system-name_granularity-clones-threshold-normsource.xml)"
    echo "  e.g., GetNormSource systems/c/linux_functions.xml systems/c/linux_functions-clones/linux_functions-clones-0.2.xml"
    exit 99
fi

# Get path of clone results file
basename="${clonesfile%%.xml}"

# OK, let's run it
echo "${lib}/tools/getnormsource.x ${pcfile} ${basename}.xml ${basename}-normsource.xml"
time ${lib}/tools/getnormsource.x "${pcfile}" "${basename}.xml" "${basename}-normsource.xml"
result=$?

if [ ${result} != 0 ]
then
    exit $result
fi

echo ""
date
echo ""

exit 0
