Introduction
============
https://github.com/elliotwoods/VVVV.Nodes.OpenCV

A new link type for VVVV 'CVImageLink' which lets you work with images on the CPU (previously achieved with DirectShow filters).
CVImageLink includes lots of lovely magic, like threading, double buffering, automatic conversion, yada yada

Perhaps all you want to know is 'is it f*&cking fast'?
the answer is yes :)

This is designed for general use of OpenCV functions + also video playback / capture.

Elliot

Credits
=======
As always, big thanks for vux for his undeniable contribution to the VVVV plugin ecosystem, both in terms of generating plugins for users and making new things possible for plugin devs (like me!)
Thanks to the VVVV devs for making such a lovely plugin architecture and a great piece of software (www.vvvv.org)

Otherwise:
Me - Elliot Woods
alg - Vadim Smakhtin

License
=======
This plugin is distributed under the LGPL license (as much as it can be)

Since we wrap EmguCV, currently we inherit the GPL license from EmguCV.

EmguCV is dual license (GPL/commercial). So to the extent to which that affects this code, this also implies that your code must be GPL if you utilise this plugin base
Suggest move to opencvsharp http://code.google.com/p/opencvsharp/
or discussion with EmguCV about a licensing model for the VVVV community. 

Isntallation
============
TODO: make notes for quick installation :)

Operating notes
===============

Known issues
------------
* Changing slice counts in image inputs/outputs will sometimes cause crashes. Fixing this is somewhat of a priority, and relates to things being properly disposed (which is also of general importance).
* Changing slice counts on processor nodes (generators, filters, destinations) doesn't result in input properties being updated for the instances.
* We don't handle stride (useful for non '4 byte' image types at non power of 2 resolutions)


Video playback
--------------
This isn't necessarilly the best route for video playback, but can also work quite nicely.

Notes for video playback:

* This doesn't employ any fancy hardware optimisation of video codecs. If you need to chunk big video files, suggest sticking with FileStream + clever codecs.
* No audio, probably never will have (although other nodes which output CVImage type could give audio).
* Works with pretty much all the AVI's i threw at it (which isn't that many on this PC). Feedback welcome!


Types
-----
We are now using CVImageLink which is a polymorphic (accepts different formats of image), double threaded (handles threading, locking) and auto converting (e.g. for RGBA8 for GPU) image type.

To use this type yourself, check out examples of where we use CVImageInputSpread and CVImageInputSpreadWith<T>

It can be used for lots of things. We can make nodes in minutes to supply video from lots of sources (e.g. CLEye, Point Grey, BlackMagic) and then use them with the full chain of CV / texture utils.


Threading
---------
Current design is 'Highly Threaded' or 'Background' as described in the threading options list at
http://vvvv.org/forum/replacing-directshow-with-managed-opencv.-video-playback-capture-cv

Each node has it's own thread, every exchange of image data between threads results in a double buffer
This obviously leads to many threads + much memory usage (perhaps in the future users will be able to select the global threading model at runtime)

The node thread has an output buffer (single).
When the thread is ready to send (i.e. all of its inputs are fresh) then it calls FOutputBuffer.SetImage(...) which pushes the image downstream.
FOutput should not have any internal buffers, instead passing through its input directly

Links are double buffers


Memory usage
------------

Examples of memory usage:

* 640*480 ~= 300KB (VGA mono)
* 640*480*3 ~= 1MB (VGA colour)
* 640*480*16 ~= 5MB (VGA colour + alpha, 32bit float)
* 1920*1080*3 ~= 6MB (HD colour video frame)

Generally each slice at each node = 2 * the above (double buffered)

Todo
====

Interfaces
----------
* ICaptureNode
* ICaptureInstance
* IFilterNode
* IFilterInstance

Node suggestions
================

General
-------
* Add						
* Subtract	
* Queue
* Cons

Files
-----
* ImageLoad				Loads a set of images into RAM as ImageRGB's. Either use OpenCV's image loader or .NET's Bitmap class (probably quicker)
* ImageSave
* VideoSave				Built into CV so should be decent performance

Tracking
--------
* Contour

CameraCalibration
-----------------
* StereoCalibrate

Future
------
* AsImage				Convert texture to image (AsVideo in existing DirectShow). - requires texture input on plugins (i.e. not possible yet)
