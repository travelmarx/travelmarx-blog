Sonos PlayListExtractor
===

See http://blog.travelmarx.com/2010/06/simple-sonos-javascript-application.html for background. This code created by Finn Ellebaek Nielsen.

Too run do the following:

1. Create directory structure:
..\PlayListExtractor
..\PlayListExtractor\src\travelmarx\PlayListExtractor.java
..\PlayListExtractor\lib\commons-lang3-3.3.2.jar
..\PlayListExtractor\build\classes

1. Compile:
..\PlayListExtractor>javac -classpath lib\* -d build\classes src\travelmarx\PlayListExtractor.java

1. Run it:
..\PlayListExtractor>java -classpath build\classes;lib\* travelmarx.PlayListExtractor 192.168.2.225 c:\Travelmarx queue
