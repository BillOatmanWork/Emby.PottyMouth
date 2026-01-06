# Emby.PottyMouth
Emby plugin to mute undesired words during playback of recordings.

When a recording is played, this plugin will look for a EDL file and mute audio and/or skip past undesired sections of the recording.
The file needs to be named the same as the recording but with extension .edl, and in the same directory as the recording.
So for example if the recording is
```
My.Little.Pony.mkv
```

The EDL file would be
```
My.Little.Pony.edl
```

I also created a utility named PMFileGenerator (https://github.com/BillOatmanWork/PMFileGenerator) to build EDL files from SRT (subtitle) files.
It creates EDL files with entries such as:
```
163.46	167.09	1
```

The 1 at the end signifies that the audio should be muted by the plugin. The first 2 entries are the start and end times, in seconds.
To skip past a section of the recording altogether, add a similar entry to the file (entries on the line are tab delimited), but replace the 1 at the end with a 0 (zero). For example
```
300.00  420.00  0
```

will tell the plugin to skip the video starting at 5 minutes and resuming at 7 minutes.

The plugin needs to be installed manually, it is not in the plugin catalog (at least not at this time). Take the released DLL file, or create your own DLL file from the source, and put it in the plugin directory along with all of your other plugins. No other files are required.

I came to realize this plugin is at the mercy of both network latency AND the accuracy of the srt file. So I added configurable offsets to start the mute ahead of the start point and after the end point.  These can be set in the plugins settings page (these values are in milliseconds).  You'll want to adjust to taste, but the numbers that work best for me are start of 1000 and end of 0.

## How to Install the Plugin
1. Download the latest `Pottymouth.dll` from the [Releases](../../releases) page
2. Copy it to your Emby plugins folder:
   - **Windows**: `C:\Users\[YourUser]\AppData\Roaming\Emby-Server\plugins`
   - **Linux**: `/var/lib/emby/plugins`
3. Restart Emby Server

### Important Safety Tip
The Emby client(s) you use must support the mute remote command in order for the plugin to be able to mute the audio. Most seem to, at the time of this writing I only know of two that do not support the command, IPhone and Roku.  And the Roku is currently being evaluated to add the command. You can watch and lend support to this thread if you would like the Roku supported.  
https://emby.media/community/index.php?/topic/109759-add-mute-command-support/#comment-1156560

If you see the text 
```
Mute is not supported.  Not possible to mute out the desired audio.
```

In the log, you'll know the device you were using does not support mute.

### Important Safety Tip 2
This implementation is not perfect.  Plugins do not have fine control of the audio.  If/when core Emby decides to support something like this, they will be able to do a much more accurate job.  But until that support happens, hopefully this will be of value to those who desire the functionality.
