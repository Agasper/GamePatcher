GamePatcher
===========

A game patcher for Windows/PC games. Can patch huge files with small patch.

This page contains an installation and using guide

##Patcher

In root of builded patcher create folder 'patcher'. It must contain next files:
* configuration.xml - a patcher configuration file
* game_logo.png  - 248x338 game logo displayed at the form
* publisher_logo.png  - 248x112 publisher logo displayed at the form
* version.txt - file that contain 1 integer digit defining current version number

##configuration.xml


```
#!xml
<?xml version="1.0"?>
<root>
	<!-- used in form title -->
	<game_name>TestGame</game_name>
	<!-- launches when play button pressed -->
	<game_exe>Test.exe</game_exe>
	<!-- opens in default browser when game logo clicked-->
	<game_url>http://coolgame.com</game_url>
	<!-- URL of file contains last version of game client-->
	<check_version_url>http://coolgame.com/version.txt</check_version_url>
	<!-- URL of directory contains patched-->
	<patches_directory>http://coolgame.com/patches/</patches_directory>
	<!-- URL with news page-->
	<news_url>http://coolgame.com/news_for_patcher.html</news_url>
	<!-- opens in default browser when publisher logo clicked-->
	<publisher_url>http://coolpublisher.com</publisher_url>
</root>
```

##Patch builder

Create `source` directory in the root of patch builder. Place game clients in folders with name of it's version. Game clients must contains configured patcher with correct version.txt file matching with the index of folder (name).
For example:

![example](http://s9.postimg.org/nczm2vryz/folders.png)


Run patch builder and select from which to which version build a patch. Step must be one, because patcher supports only incremental update.
Click "Make patch".
Result patch will be placed in output folder with name like 1_2.patch

P.S. You may place this patch to any web hosting (even Dropbox)

Code based on this https://github.com/einaros/RsyncNet

License: MIT
