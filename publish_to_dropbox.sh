#!/bin/bash
# echo Running $BASH_SOURCE
# set | egrep GIT
# echo PWD is $PWD

if [ -z "$1" ]; then
	echo "Need to specify a root directory for saving files."
	echo "Exiting..."
	exit
fi

echo "Saving output to $1"

# ensure on master before publishing
if [ `git rev-parse --abbrev-ref HEAD` == "master" ]; then
   # publish Unit 1 DoThis and Completed
   git archive -o "$1/Unit1-DoThis.zip" HEAD:src/Unit-1/DoThis/
   git archive -o "$1/Unit1-Completed.zip" HEAD:src/Unit-1/lesson6/

   # publish Unit 2 DoThis and Completed
   git archive -o "$1/Unit2-DoThis.zip" HEAD:src/Unit-2/DoThis/
   git archive -o "$1/Unit2-Completed.zip" HEAD:src/Unit-2/lesson5/

   # publish Unit 3 DoThis and Completed
   git archive -o "$1/Unit3-DoThis.zip" HEAD:src/Unit-3/DoThis/
   git archive -o "$1/Unit3-Completed.zip" HEAD:src/Unit-3/lesson5/
fi