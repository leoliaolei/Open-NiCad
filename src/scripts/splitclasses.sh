#!/bin/bash
# Split clone classes into folders and files

# Revised 1.10.18

ulimit -s hard

# Find our installation
lib="${0%%/scripts/splitclasses.sh}"
if [ ! -d ${lib} ]
then
    echo "*** Error:  cannot find NiCad installation ${lib}"
    echo ""
    exit 99
fi

date

for classfile in $*
do
    # Check we have a system
    if [ ! -f "$classfile" ] 
    then
	echo "Usage:  SplitClasses system-name_granularity-clones/system-name_granularity-clones-threshold-classes-withsource.xml"
	echo "  (Output in system-name_granularity-clones/system-name_granularity-clones-threshold-classes-withsource/*)"
	echo "  e.g., SplitClasses systems/c/linux_functions-clones/linux_functions-clones-0.2-classes-withsource.xml"
	exit 99
    fi

    # OK, let's run it
    echo "${lib}/tools/splitclasses.x ${classfile}"
    time ${lib}/tools/splitclasses.x "${classfile}"
    result=$?

    if [ ${result} != 0 ]
    then
	exit $result
    fi
done

echo ""
date
echo ""

exit 0
