param ([string]$File = $(throw "File is required"))
cat $File -tail 1 -wait
