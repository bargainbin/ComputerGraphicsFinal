<?xml version="1.0" encoding="UTF-8"?>
<MiddleVR>
	<Kernel Version="2.2.0.0" />
	<DeviceManager>
		<Driver Type="vrDriverDirectInput" />
		<Wand Name="Wand0" Driver="0" Axis="0" HorizontalAxis="0" HorizontalAxisScale="1" VerticalAxis="1" VerticalAxisScale="1" AxisDeadZone="0.3" Buttons="Mouse.Buttons" Button0="0" Button1="1" Button2="2" Button3="3" Button4="4" Button5="5" />
	</DeviceManager>
	<DisplayManager WindowMode="1" CompositorMode="0" AlwaysOnTop="1" ShowMouseCursor="0" VSync="0" AntiAliasing="4" ForceHideTaskbar="0">
		<Node3D Name="SystemCenter" Parent="None" Tracker="TrackerSimulatorMouse0.Tracker" IsFiltered="0" Filter="0" UseTrackerX="1" UseTrackerY="1" UseTrackerZ="1" UseTrackerYaw="1" UseTrackerPitch="1" UseTrackerRoll="1" />
		<Node3D Name="Screens" Parent="SystemCenter" Tracker="0" IsFiltered="0" Filter="0" PositionLocal="0.000000,0.000000,0.000000" OrientationLocal="0.000000,0.000000,0.000000,1.000000" />
		<Screen Name="ScreenFront" Parent="Screens" Tracker="0" IsFiltered="0" Filter="0" PositionLocal="0.000000,1.200000,0.000000" OrientationLocal="0.000000,0.000000,0.000000,1.000000" Width="3.2" Height="2.4" />
		<Screen Name="ScreenLeft" Parent="Screens" Tracker="0" IsFiltered="0" Filter="0" PositionLocal="-1.600000,-0.400000,0.000000" OrientationLocal="0.000000,0.000000,0.707107,0.707106" Width="3.2" Height="2.4" />
		<Screen Name="ScreenRight" Parent="Screens" Tracker="0" IsFiltered="0" Filter="0" PositionLocal="1.600000,-0.400000,0.000000" OrientationLocal="0.000000,0.000000,-0.707107,0.707106" Width="3.2" Height="2.4" />
		<Node3D Name="Cameras" Parent="SystemCenter" Tracker="0" IsFiltered="0" Filter="0" PositionLocal="0.000000,0.000000,0.000000" OrientationLocal="0.000000,0.000000,0.000000,1.000000" />
		<Camera Name="CameraFront" Parent="Cameras" Tracker="0" IsFiltered="0" Filter="0" PositionLocal="0.000000,0.000000,0.000000" OrientationLocal="0.000000,0.000000,0.000000,1.000000" Screen="ScreenFront" Warper="0" VerticalFOV="60" Near="0.1" Far="1000" ScreenDistance="1" UseViewportAspectRatio="0" AspectRatio="1.33333" />
		<Camera Name="CameraLeft" Parent="Cameras" Tracker="0" IsFiltered="0" Filter="0" PositionLocal="0.000000,0.000000,0.000000" OrientationLocal="0.000000,0.000000,0.707107,0.707106" Screen="ScreenLeft" Warper="0" VerticalFOV="60" Near="0.1" Far="1000" ScreenDistance="1" UseViewportAspectRatio="0" AspectRatio="1.33333" />
		<Camera Name="CameraRight" Parent="Cameras" Tracker="0" IsFiltered="0" Filter="0" PositionLocal="0.000000,0.000000,0.000000" OrientationLocal="0.000000,0.000000,-0.707107,0.707106" Screen="ScreenRight" Warper="0" VerticalFOV="60" Near="0.1" Far="1000" ScreenDistance="1" UseViewportAspectRatio="0" AspectRatio="1.33333" />
		<Viewport Name="Viewport_Front" Left="640" Top="0" Width="640" Height="480" Camera="CameraFront" ClusterNode="0" Stereo="0" StereoMode="0" CompressSideBySide="0" StereoInvertEyes="0" UseCustomStereoCameras="0" CustomLeftBufferCamera="0" CustomRightBufferCamera="0" OffScreen="0" />
		<Viewport Name="Viewport_Left" Left="0" Top="0" Width="640" Height="480" Camera="CameraLeft" ClusterNode="0" Stereo="0" StereoMode="0" CompressSideBySide="0" StereoInvertEyes="0" UseCustomStereoCameras="0" CustomLeftBufferCamera="0" CustomRightBufferCamera="0" OffScreen="0" />
		<Viewport Name="Viewport_Right" Left="1280" Top="0" Width="640" Height="480" Camera="CameraRight" ClusterNode="0" Stereo="0" StereoMode="0" CompressSideBySide="0" StereoInvertEyes="0" UseCustomStereoCameras="0" CustomLeftBufferCamera="0" CustomRightBufferCamera="0" OffScreen="0" />
	</DisplayManager>
	<Scripts>
		<Script Type="TrackerSimulatorMouse" Name="TrackerSimulatorMouse0" />
	</Scripts>
	<ClusterManager NVidiaSwapLock="0" ServerUnityWindow="0" ClientConnectionTimeout="60" />
</MiddleVR>
