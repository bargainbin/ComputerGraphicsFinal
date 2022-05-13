/* MVRTools
 * MiddleVR
 * (c) MiddleVR
 */

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace MiddleVR.Unity
{
    public partial class MVRTools
    {
        #region Viewport Creation
        /// <summary>
        /// Setup the output window and all cameras in the scene
        /// </summary>
        /// <param name="iUsingAntiAliasing">Enable Anti-Aliasing</param>
        public static void CreateViewportsAndCameras(
            GameObject mvrManager)
        {
            MVRTools.Log(VRLogLevel.Debug, "[>] Starting to create viewports.");

            VRWindowMode realOutputMode = MVR.DisplayMgr.GetWindowMode();

            if (Application.isEditor && realOutputMode == VRWindowMode.Proxy)
            {
                MVRTools.Log(VRLogLevel.Info, "[ ] D3D11 Proxy is not supported in editor mode. Quad-buffer cameras will only show the left eye.");
                realOutputMode = VRWindowMode.UnityWindow;
            }

            if (realOutputMode == VRWindowMode.Proxy &&
                SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D11)
            {
                MVRTools.Log(VRLogLevel.Error, "[ ] D3D11 Proxy is only supported with Direct3D11 Rendering. Quad-buffer cameras will only show the left eye.");
                realOutputMode = VRWindowMode.UnityWindow;
            }

            VRLogLevel logLevel = VRLogLevel.Critical;

            if (MVR.ClusterMgr.IsClient())
            {
                logLevel = VRLogLevel.Error;
            }

#if UNITY_2019_3_OR_NEWER
            if (realOutputMode == VRWindowMode.Compositor &&
                SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 &&
                SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D12)
            {
                MVRTools.Log(logLevel, "[ ] Compositor is only supported with Direct3D11 or Direct3D12 Rendering. Switching to UnityWindow. Quad-buffer cameras will only show the left eye.");
                realOutputMode = VRWindowMode.UnityWindow;
            }
#else
            if (realOutputMode == VRWindowMode.Compositor)
            {
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D11
                    && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D12)
                {
                    MVRTools.Log(logLevel, "[X] Compositor is only supported with Direct3D11 Rendering (or Direct12 12 since Unity 2019.3). Switching to UnityWindow. Quad-buffer cameras will only show the left eye.");
                    realOutputMode = VRWindowMode.UnityWindow;
                }

                if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12)
                {
                    MVRTools.Log(logLevel, "[X] Direct3D 12 is only supported by MiddleVR since Unity 2019.3");
                    realOutputMode = VRWindowMode.UnityWindow;
                }
            }
#endif

            var windowRect = new RectInt
            {
                x = MVR.DisplayMgr.GetWindowLeft(),
                y = MVR.DisplayMgr.GetWindowTop(),
                width = (int)MVR.DisplayMgr.GetWindowWidth(),
                height = (int)MVR.DisplayMgr.GetWindowHeight()
            };

            Compositor compositor = mvrManager.GetComponent<Compositor>();
            if (compositor != null)
            {
                UnityEngine.Object.Destroy(compositor);
                compositor = null;
            }

            var vsync = GetVSync();
            QualitySettings.vSyncCount = vsync.unity ? 1 : 0;
            QualitySettings.antiAliasing = (int)MVR.DisplayMgr.GetAntiAliasing();

            if (realOutputMode == VRWindowMode.Compositor)
            {
                compositor = mvrManager.AddComponent<Compositor>();
                compositor.WindowTitle = "MiddleVR Compositor";
                compositor.WindowGeometry = windowRect;
                compositor.WindowBorders = false;

                if(Application.isEditor)
                {
                    compositor.WindowAlwaysOnTop = false;
                }
                else
                {
                    compositor.WindowAlwaysOnTop = MVR.DisplayMgr.IsWindowAlwaysOnTop();
                }

                compositor.VSync = vsync.viewportOutput;
                compositor.NVSwapLock = MVR.ClusterMgr.GetNVidiaSwapLock();
                compositor.QuadBuffer = MVR.DisplayMgr.GetWindowQuadBuffer();
                compositor.NDI = MVR.DisplayMgr.GetNDI();

                int AA = 1;
                int mvrAA = (int)MVR.DisplayMgr.GetAntiAliasing();

                if (mvrAA == 1 || mvrAA == 2 || mvrAA == 4 || mvrAA == 8 )
                {
                    AA = mvrAA;
                }

                compositor.AntiAliasing = AA;

                switch(MVR.DisplayMgr.GetCompositorMode())
                {
                    case VRCompositorMode.Direct3D11:
                        compositor.Mode = Compositor.CompositorMode.Direct3D11;
                        break;
                    case VRCompositorMode.OpenGL:
                        compositor.Mode = Compositor.CompositorMode.OpenGL;
                        break;
                    default:
                        compositor.Mode = Compositor.CompositorMode.Auto;
                        break;
                }
            }

            var viewportOutputOptions = new OutputConfiguration
            (
                outputMode: realOutputMode,
                quadBuffer: MVR.DisplayMgr.GetWindowQuadBuffer(),
                antiAliasing: (int)MVR.DisplayMgr.GetAntiAliasing(),
                compositor: compositor
            );

            SetupUnityWindow(realOutputMode, windowRect);


            List<vrViewport> viewportList;
            var clusterNode = MVR.ClusterMgr.GetMyClusterNode();
            if (clusterNode != null)
            {
                if (clusterNode.GetViewportsNb() == 0)
                {
                    MVRTools.Log(VRLogLevel.Error, $"[X] Couldn't find a viewport for my cluster node '{clusterNode.GetName()}'!");
                }
                viewportList = clusterNode.Viewports.ToList();
            }
            else
            {
                if (MVR.DisplayMgr.GetViewportsNb() == 0)
                {
                    MVRTools.Log(VRLogLevel.Error, $"[X] Couldn't find a viewport!");
                }
                viewportList = MVR.DisplayMgr.Viewports.ToList();
            }

            foreach (var viewport in viewportList)
            {
                CreateViewport(viewportOutputOptions, viewport, windowRect);
            }

            MVRTools.Log(3, "[<] End of viewport creation.");
        }

        /// <summary>
        /// Output configuration for this instance of Unity
        /// </summary>
        private class OutputConfiguration
        {
            public OutputConfiguration(VRWindowMode outputMode, bool quadBuffer, int antiAliasing, Compositor compositor)
            {
                OutputMode = outputMode;
                QuadBuffer = quadBuffer;
                AntiAliasing = antiAliasing;
                Compositor = compositor;
            }

            public readonly VRWindowMode OutputMode;
            public readonly bool QuadBuffer;
            public readonly int AntiAliasing;
            public readonly Compositor Compositor;
        }

        /// <summary>
        /// The target eye for a camera
        /// </summary>
        private enum Eye
        {
            /// <summary>
            /// Camera targets a normal (non-quadbuffer) output
            /// </summary>
            Mono,

            /// <summary>
            /// Camera targets the left eye on a quadbuffer output
            /// </summary>
            Left,

            /// <summary>
            /// Camera targets the right eye on a quadbuffer output
            /// </summary>
            Right
        }

        /// <summary>
        /// Orientation of a SideBySide (split) stereo viewport
        /// </summary>
        private enum SideBySideOrientation
        {
            None,
            Horizontal,
            Vertical
        }

        /// <summary>
        /// Compression of a SideBySide (split) stereo viewport
        /// </summary>
        private enum SideBySideCompression
        {
            Disabled,
            Enabled
        }

        /// <summary>
        /// Setups the Unity window size and fullscreen mode
        /// </summary>
        /// <param name="outputMode"></param>
        /// <param name="windowRect"></param>
        /// <param name="fullscreen"></param>
        private static void SetupUnityWindow(VRWindowMode outputMode, RectInt windowRect)
        {
            if (outputMode == VRWindowMode.Proxy || outputMode == VRWindowMode.Compositor)
            {
                // Using active stereo so we don't need to set the screen
                // resolution.
                if (!Application.isEditor)
                {
                    MVRTools.Log(VRLogLevel.Debug, "[ ] This output mode will create a new window, so hiding the Unity one.");
                    MVR.DisplayMgr.HideUnityWindow();
                }
            }
            else // VRWindowMode.UnityWindow
            {
                var resolution = Screen.currentResolution;
                MVRTools.Log(3, $"[ ] Current resolution: {resolution.width}x{resolution.height} ({resolution.refreshRate}Hz).");

                MVRTools.Log(VRLogLevel.Debug, "[>] Starting listing of available screen resolutions.");
                foreach (var availableResol in Screen.resolutions)
                {
                    MVRTools.Log(VRLogLevel.Debug, $"[ ] {availableResol.width}x{availableResol.height} ({availableResol.refreshRate}Hz).");
                }
                MVRTools.Log(VRLogLevel.Debug, "[<] End of available screen resolutions.");
                MVRTools.Log(VRLogLevel.Debug, $"[ ] Setting resolution to {windowRect.width}x{windowRect.height} {"in window mode"}.");

                Screen.SetResolution(windowRect.width, windowRect.height, false);
            }
        }

        private static (bool unity, bool viewportOutput) GetVSync()
        {
            if (MVR.ClusterMgr.IsServer() && MVR.ClusterMgr.GetServerUnityWindow())
            {
                MVR.Log(VRLogLevel.Info, "[ ] Unity: ServerUnityWindow: Disable VSync on Cluster Master.");
                return (unity: false, viewportOutput: false);
            }
            else if (!MVR.DisplayMgr.GetVSync())
            {
                MVR.Log(VRLogLevel.Info, "[ ] Unity: Disabling VSync.");
                return (unity: false, viewportOutput: false);
            }
            else if (MVR.DisplayMgr.GetWindowMode() != VRWindowMode.UnityWindow)
            {
                MVR.Log(VRLogLevel.Info, "[ ] Unity: Disabling VSync in Unity, it will be handled by the viewport output.");
                return (unity: false, viewportOutput: true);
            }
            else
            {
                MVR.Log(VRLogLevel.Info, "[ ] Unity: Enabling VSync.");
                return (unity: true, viewportOutput: true);
            }
        }

        /// <summary>
        /// Setups one viewport
        /// </summary>
        /// <param name="outputConfiguration">Current output configuration</param>
        /// <param name="viewport"></param>
        /// <param name="windowRect">Window output coordinates</param>
        private static void CreateViewport(
            [NotNull] OutputConfiguration outputConfiguration,
            [NotNull] vrViewport viewport,
            RectInt windowRect)
        {
            MVRTools.Log(VRLogLevel.Debug, $"[ ] Evaluating viewport '{viewport.GetName()}'.");

            var vpRect = new RectInt
            {
                x = viewport.GetLeft(),
                y = viewport.GetTop(),
                width = (int)viewport.GetWidth(),
                height = (int)viewport.GetHeight()
            };

            if (viewport.GetStereo() && viewport.GetStereoMode() == VRStereoMode.QuadBuffer)
            {
                var (cameraLeft, cameraRight) = GetViewportStereoCameras(viewport);
                if (cameraLeft == null || cameraRight == null)
                {
                    MVRTools.Log(VRLogLevel.Error, $"[X] Couldn't find cameras for viewport '{viewport.GetName()}'. Stopping setup of this viewport.");
                    return;
                }

                CreateQuadBufferViewport(
                    outputConfiguration,
                    cameraLeft,
                    cameraRight,
                    vpRect,
                    windowRect);
            }
            else if (viewport.GetStereo() && viewport.GetStereoMode() == VRStereoMode.SideBySide)
            {
                var (cameraLeft, cameraRight) = GetViewportStereoCameras(viewport);
                if (cameraLeft == null || cameraRight == null)
                {
                    MVRTools.Log(VRLogLevel.Error, $"[X] Couldn't find cameras for viewport '{viewport.GetName()}'. Stopping setup of this viewport.");
                    return;
                }

                CreateSideBySideViewport(
                    outputConfiguration,
                    cameraLeft,
                    cameraRight,
                    vpRect,
                    windowRect,
                    SideBySideOrientation.Horizontal,
                    viewport.GetCompressSideBySide() ? SideBySideCompression.Enabled : SideBySideCompression.Disabled);

            }
            else if (!viewport.GetStereo())
            {
                var camera = viewport.GetCamera();
                if (camera == null)
                {
                    MVRTools.Log(VRLogLevel.Error, $"[X] Couldn't find camera for viewport '{viewport.GetName()}'. Stopping setup of this viewport.");
                    return;
                }

                CreateMonoViewport(
                    outputConfiguration,
                    camera,
                    vpRect,
                    windowRect);
            }
            else
            {
                MVRTools.Log(VRLogLevel.Error, $"[X] Invalid options for viewport '{viewport.GetName()}'. Stopping setup of this viewport.");
            }
        }

        /// <summary>
        /// Get the "Left" and "Right" cameras of any stereo viewport,
        /// taking into account custom stereo cameras and invert eyes parameters
        /// </summary>
        /// <param name="viewport">vrViewport reference</param>
        /// <returns>(LeftCamera, RightCamera)</returns>
        private static (vrCamera cameraLeft, vrCamera cameraRight) GetViewportStereoCameras([NotNull] vrViewport viewport)
        {
            vrCamera cameraLeft;
            vrCamera cameraRight;

            if (viewport.GetUseCustomStereoCameras())
            {
                cameraLeft = viewport.GetCustomLeftBufferCamera();
                cameraRight = viewport.GetCustomRightBufferCamera();
            }
            else
            {
                var camera = viewport.GetCamera();
                if (camera == null)
                {
                    MVRTools.Log(VRLogLevel.Error, "[X] Viewport using null camera. Viewport will be black.");
                    return (null, null);
                }

                var cameraStereo = camera as vrCameraStereo;
                if (cameraStereo == null)
                {
                    MVRTools.Log(VRLogLevel.Error, "[X] Stereo viewport using a non-stereo camera. Viewport will be black.");
                    return (null, null);
                }

                cameraLeft = cameraStereo.GetCameraLeft();
                cameraRight = cameraStereo.GetCameraRight();
            }

            if (viewport.GetStereoInvertEyes())
            {
                return (cameraRight, cameraLeft);
            }
            else
            {
                return (cameraLeft, cameraRight);
            }
        }

        /// <summary>
        /// Setups a mono viewport
        /// </summary>
        /// <param name="outputConfiguration">Current output configuration</param>
        /// <param name="camera"></param>
        /// <param name="vpRect"></param>
        /// <param name="windowRect"></param>
        private static void CreateMonoViewport(
            [NotNull] OutputConfiguration outputConfiguration,
            [NotNull] vrCamera camera,
            RectInt vpRect,
            RectInt windowRect)
        {
            var cameraComponent = MVRNodesMapper.Instance.GetNode(camera).GetComponent<Camera>();
            CreateViewportSingleEye(outputConfiguration, camera, cameraComponent, vpRect, windowRect, Eye.Mono, SideBySideOrientation.None, SideBySideCompression.Disabled);
        }

        /// <summary>
        /// Setups a SideBySide viewport
        /// </summary>
        /// <param name="outputConfiguration">Current output configuration</param>
        /// <param name="cameraLeft"></param>
        /// <param name="cameraRight"></param>
        /// <param name="vpRect"></param>
        /// <param name="windowRect"></param>
        /// <param name="sideBySideOrientation"></param>
        /// <param name="sideBySideCompression"></param>
        private static void CreateSideBySideViewport(
            [NotNull] OutputConfiguration outputConfiguration,
            [NotNull] vrCamera cameraLeft,
            [NotNull] vrCamera cameraRight,
            RectInt vpRect,
            RectInt windowRect,
            SideBySideOrientation sideBySideOrientation,
            SideBySideCompression sideBySideCompression)
        {
            RectInt leftRect = vpRect;
            if (sideBySideOrientation == SideBySideOrientation.Horizontal)
            {
                leftRect.width = vpRect.width / 2;
            }
            else if (sideBySideOrientation == SideBySideOrientation.Vertical)
            {
                leftRect.height = vpRect.height / 2;
            }

            var cameraComponentLeft = MVRNodesMapper.Instance.GetNode(cameraLeft).GetComponent<Camera>();
            CreateViewportSingleEye(outputConfiguration, cameraLeft, cameraComponentLeft, leftRect, windowRect, Eye.Mono, sideBySideOrientation, sideBySideCompression);

            RectInt rightRect = vpRect;
            if (sideBySideOrientation == SideBySideOrientation.Horizontal)
            {
                rightRect.x += vpRect.width / 2;
                rightRect.width = vpRect.width / 2;
            }
            else if (sideBySideOrientation == SideBySideOrientation.Vertical)
            {
                rightRect.y += vpRect.height / 2;
                rightRect.height = vpRect.height / 2;
            }

            var cameraComponentRight = MVRNodesMapper.Instance.GetNode(cameraRight).GetComponent<Camera>();
            CreateViewportSingleEye(outputConfiguration, cameraRight, cameraComponentRight, rightRect, windowRect, Eye.Mono, sideBySideOrientation, sideBySideCompression);
        }

        /// <summary>
        /// Setups a QuadBuffer viewport
        /// </summary>
        /// <param name="outputConfiguration">Current output configuration</param>
        /// <param name="cameraLeft"></param>
        /// <param name="cameraRight"></param>
        /// <param name="vpRect"></param>
        /// <param name="windowRect"></param>
        private static void CreateQuadBufferViewport(
            [NotNull] OutputConfiguration outputConfiguration,
            [NotNull] vrCamera cameraLeft,
            [NotNull] vrCamera cameraRight,
            RectInt vpRect,
            RectInt windowRect)
        {
            var cameraComponentLeft = MVRNodesMapper.Instance.GetNode(cameraLeft).GetComponent<Camera>();
            CreateViewportSingleEye(outputConfiguration, cameraLeft, cameraComponentLeft, vpRect, windowRect, Eye.Left, SideBySideOrientation.None, SideBySideCompression.Disabled);

            var cameraComponentRight = MVRNodesMapper.Instance.GetNode(cameraRight).GetComponent<Camera>();
            CreateViewportSingleEye(outputConfiguration, cameraRight, cameraComponentRight, vpRect, windowRect, Eye.Right, SideBySideOrientation.None, SideBySideCompression.Disabled);
        }

        /// <summary>
        /// Configure a Camera behaviour for a single eye vrCamera
        /// </summary>
        /// <param name="outputConfiguration">Current output configuration</param>
        /// <param name="camera">vrCamera instance</param>
        /// <param name="cameraComponent">Unity Camera component corresponding to the vrCamera</param>
        /// <param name="vpRect">Viewport output rectangle (absolute coordinates)</param>
        /// <param name="windowRect">Window output rectangle (absolute coordinates)</param>
        /// <param name="eye">Target eye of the camera</param>
        /// <param name="sideBySideOrientation">Orientation of the SideBySide viewport</param>
        /// <param name="sideBySideCompression">Compression of the SideBySide viewport</param>
        private static void CreateViewportSingleEye(
            [NotNull] OutputConfiguration outputConfiguration,
            [NotNull] vrCamera camera,
            [NotNull] Camera cameraComponent,
            RectInt vpRect,
            RectInt windowRect,
            Eye eye,
            SideBySideOrientation sideBySideOrientation,
            SideBySideCompression sideBySideCompression)
        {
            //
            // Check eye
            //

            // Unity is not capable of outputting stereo in its editor window so we only show the left eye
            if (outputConfiguration.OutputMode == VRWindowMode.UnityWindow && eye != Eye.Mono)
            {
                if (eye == Eye.Left)
                {
                    eye = Eye.Mono;
                }
                else if (eye == Eye.Right)
                {
                    MVRTools.Log(VRLogLevel.Warning, $"[~] Unity: Skipping setup of {camera.GetName()}: Right eye is not renderer in a Unity window.");
                    return;
                }
            }

            if (eye == Eye.Mono && outputConfiguration.QuadBuffer)
            {
                MVRTools.Log(VRLogLevel.Error, $"[X] {camera.GetName()}: Trying to render a mono viewport in a quadbuffer window! Viewport will be black.");
                return;
            }
            else if (eye != Eye.Mono && !outputConfiguration.QuadBuffer)
            {
                MVRTools.Log(VRLogLevel.Error, $"[X] {camera.GetName()}: Trying to render a quadbuffer viewport in a mono window! Viewport will be black.");
                return;
            }

            //
            // Set viewport coordinates
            //

            // Set Unity's camera rectangle or use camera's own aspect ratio otherwise.
            MVRTools.Log(VRLogLevel.Debug, $"[ ] Setting-up viewport for camera '{camera.GetName()}' ({vpRect.x},{vpRect.y})({vpRect.width}x{vpRect.height}).");

            if (outputConfiguration.OutputMode == VRWindowMode.UnityWindow)
            {
                var camRect = new Rect
                {
                    x = (float)(vpRect.x - windowRect.x) / (float)windowRect.width,
                    y = 1.0f - (float)(vpRect.y - windowRect.y + vpRect.height) / (float)windowRect.height,
                    width = (float)vpRect.width / (float)windowRect.width,
                    height = (float)vpRect.height / (float)windowRect.height
                };

                cameraComponent.rect = camRect;

                MVRTools.Log(VRLogLevel.Debug, $"[ ] Viewport normalized rect: ({camRect.x},{camRect.y})({camRect.width}x{camRect.height}).");
            }

            //
            // Set camera aspect ratio
            //

            float aspectRatio;

            if (camera.GetUseViewportAspectRatio())
            {
                aspectRatio = (float)vpRect.width / (float)vpRect.height;
                MVRTools.Log(VRLogLevel.Debug, $"[ ] Camera uses viewport aspect ratio: {aspectRatio}.");
            }
            else
            {
                aspectRatio = camera.GetAspectRatio();
                MVRTools.Log(VRLogLevel.Debug, $"[ ] Camera uses its own aspect ratio: {aspectRatio}.");
            }

            if (sideBySideCompression == SideBySideCompression.Enabled)
            {
                if (sideBySideOrientation == SideBySideOrientation.Horizontal)
                {
                    MVRTools.Log(VRLogLevel.Debug, "[ ] Compressed Side-by-Side (Horizontal).");
                    // Side-by-side will be displayed on only one display but
                    // will be stretched (scaled) by the system.
                    aspectRatio *= 2.0f;
                }
                if (sideBySideOrientation == SideBySideOrientation.Vertical)
                {
                    MVRTools.Log(VRLogLevel.Debug, "[ ] Compressed Side-by-Side (Vertical).");
                    // Side-by-side will be displayed on only one display but
                    // will be stretched (scaled) by the system.
                    aspectRatio /= 2.0f;
                }
                else
                {
                    MVRTools.Log(VRLogLevel.Debug, "[ ] Uncompressed Side-by-Side.");
                }
            }

            camera.SetAspectRatio(aspectRatio);

            //
            // Handle viewport output
            //

            if (outputConfiguration.OutputMode == VRWindowMode.Proxy)
            {
                CreateD3D11ProxyCameraRenderTarget(
                    camera,
                    cameraComponent,
                    vpRect.width,
                    vpRect.height,
                    outputConfiguration.AntiAliasing);
            }
            else if (outputConfiguration.OutputMode == VRWindowMode.Compositor)
            {
                switch (eye)
                {
                    case Eye.Left:
                        cameraComponent.stereoTargetEye = StereoTargetEyeMask.Left;
                        break;
                    case Eye.Right:
                        cameraComponent.stereoTargetEye = StereoTargetEyeMask.Right;
                        break;
                    default:
                        cameraComponent.stereoTargetEye = StereoTargetEyeMask.Both;
                        break;
                }

                outputConfiguration.Compositor.Cameras.Add(new Compositor.CameraInfo { Camera = cameraComponent, Rect = vpRect });
            }

            //
            // Finally, enable the camera
            //

            cameraComponent.enabled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="cameraComponent"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="setAntiAliasing"></param>
        /// <param name="antiAliasingLevel"></param>
        private static void CreateD3D11ProxyCameraRenderTarget(
            [NotNull] vrCamera camera,
            [NotNull] Camera cameraComponent,
            int width,
            int height,
            int antiAliasingLevel)
        {
            MVRTools.Log(VRLogLevel.Debug, $"[>] Tracking of render texture creation ({width}x{height}) for the camera '{cameraComponent.name}'.");

            MVR.DisplayMgr.StartTrackingRenderTargets((uint)width, (uint)height);
            MVR.DisplayMgr.SetRenderTargetName(cameraComponent.name);
            MVR.DisplayMgr.SetRenderTargetCameraId((int)camera.GetId());

            var renderTexture = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);


            renderTexture.antiAliasing = antiAliasingLevel;
            renderTexture.Create();

            // If we want to convert this texture to OpenGL we need to be sure
            // that it is created at this point.
            var previousActiveRenderTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;

            // Force creation of the texture by reading its first pixel.
            var forcingCreationTexture = new Texture2D(1, 1);
            forcingCreationTexture.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
            forcingCreationTexture.Apply();

            RenderTexture.active = previousActiveRenderTexture;

            MVR.DisplayMgr.StopTrackingRenderTargets();

            MVRTools.Log(VRLogLevel.Debug, $"[<] End of tracking render texture creation ({width}x{height}) for the camera '{cameraComponent.name}'.");

            cameraComponent.targetTexture = renderTexture;

            if (renderTexture.IsCreated())
            {
                cameraComponent.gameObject.AddComponent<MVRCameraCB>();
            }
            else
            {
                MVRTools.Log(VRLogLevel.Critical, $"[X] Couldn't create target texture for camera '{cameraComponent.name}'.");
            }
        }

        #endregion
    }
}
