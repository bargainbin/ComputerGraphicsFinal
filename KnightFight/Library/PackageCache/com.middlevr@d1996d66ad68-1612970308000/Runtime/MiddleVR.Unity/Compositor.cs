using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using MiddleVR;
using MiddleVR.Unity;

namespace MiddleVR.Unity
{
    public class Compositor : MonoBehaviour
    {
        public List<CameraInfo> Cameras = new List<CameraInfo>();

        [Serializable]
        public class CameraInfo
        {
            public Camera Camera;
            public RectInt Rect;
        }

        enum WarpingType
        {
            None,
            Vioso,
            ScalableDisplay
        }

        public string WindowTitle = "MiddleVR Compositor";
        public RectInt WindowGeometry = new RectInt(0, 0, 100, 100);
        public bool WindowAlwaysOnTop = false;
        public bool WindowBorders = false;
        public int AntiAliasing = 1;
        public bool VSync = true;
        public bool QuadBuffer = false;
        public bool NVSwapLock = false;
        public bool NDI = false;
        public CompositorMode Mode = CompositorMode.Auto;

        private class RegisteredCamera
        {
            public Camera Camera;
            public StereoTargetEyeMask EyeMask;
            public RectInt Rect;
            public Texture2D Surface;
            public RenderTexture RT;
        }

        private readonly List<RegisteredCamera> _registeredCameras = new List<RegisteredCamera>();

        private Coroutine _waitForEndOfFrameCoroutine = null;
        private readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

        private void Start()
        {
            if (!IsRendererSupported())
            {
#if UNITY_2019_3_OR_NEWER
                Debug.LogError("MvrCompositor only supports Direct3D11 or Direct3D12");
#else
                Debug.LogError("MvrCompositor only supports Direct3D11 (or Direct3D12 since Unity 2019.3)");
#endif
                return;
            }

            ResetCompositor();

            if (CameraListHasChanged())
            {
                ClearRegisteredCameras();
                CreateRegisteredCameras();
            }

            string folder = MVR.LogMgr.GetLogFolder();

            CreateCompositor(
                title: WindowTitle,
                x: WindowGeometry.x,
                y: WindowGeometry.y,
                width: WindowGeometry.width,
                height: WindowGeometry.height,
                borders: WindowBorders,
                alwaysOnTop: WindowAlwaysOnTop,
                logFolder: folder,
                vsync: VSync,
                nvswaplock: NVSwapLock,
                quadbuffer: QuadBuffer,
                mode: Mode,
                cameras: this._registeredCameras,
                ndi: NDI );

            if (!WaitCompositor())
                throw new InvalidOperationException("Could not start MiddleVR Compositor! Please check MiddleVR logs.");

            CreateRegisteredCamerasSurfaces();

            _waitForEndOfFrameCoroutine = StartCoroutine(WaitForEndOfFrameCoroutine());
        }

        private void OnDisable()
        {
            if (!IsRendererSupported())
                return;

            ClearRegisteredCameras();

            DestroyCompositor();

            if (_waitForEndOfFrameCoroutine != null)
            {
                StopCoroutine(_waitForEndOfFrameCoroutine);
                _waitForEndOfFrameCoroutine = null;
            }
        }

        private IEnumerator WaitForEndOfFrameCoroutine()
        {
            while (true)
            {
                yield return _waitForEndOfFrame;
                OnEndOfFrame();
            }
        }

        private bool CameraListHasChanged()
        {
            int count = Cameras.Count;
            if (count != _registeredCameras.Count)
            {
                return true;
            }
            else
            {
                for (int i = 0; i < count; ++i)
                {
                    var cameraInfo = Cameras[i];
                    var registeredCamera = _registeredCameras[i];

                    if (cameraInfo.Camera != registeredCamera.Camera ||
                        cameraInfo.Camera.stereoTargetEye != registeredCamera.EyeMask ||
                        cameraInfo.Rect.x != registeredCamera.Rect.x ||
                        cameraInfo.Rect.y != registeredCamera.Rect.y ||
                        cameraInfo.Rect.width != registeredCamera.Rect.width ||
                        cameraInfo.Rect.height != registeredCamera.Rect.height)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ClearRegisteredCameras()
        {
            foreach (var compositorSurfaceInfo in _registeredCameras)
            {
                if (compositorSurfaceInfo.Camera != null &&
                    compositorSurfaceInfo.Camera.targetTexture != null)
                {
                    Destroy(compositorSurfaceInfo.Camera.targetTexture);
                    compositorSurfaceInfo.Camera.targetTexture = null;
                }

                compositorSurfaceInfo.Camera = null;
                Destroy(compositorSurfaceInfo.Surface);
            }

            _registeredCameras.Clear();
        }

        private void CreateRegisteredCameras()
        {
            foreach (var cameraInfo in Cameras)
            {
                var renderTexture = new RenderTexture(cameraInfo.Rect.width, cameraInfo.Rect.height, 0, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = AntiAliasing
                };

                cameraInfo.Camera.targetTexture = renderTexture;

                _registeredCameras.Add(new RegisteredCamera
                {
                    Camera = cameraInfo.Camera,
                    EyeMask = cameraInfo.Camera.stereoTargetEye,
                    Rect = cameraInfo.Rect
                });
            }
        }

        private void CreateRegisteredCamerasSurfaces()
        {
            for (int i = 0; i < _registeredCameras.Count; i++)
            {
                var cam = _registeredCameras[i];
                Texture2D texture = Texture2D.CreateExternalTexture(
                                            cam.Rect.width,
                                            cam.Rect.height,
                                            TextureFormat.RGBA32,
                                            false,
                                            false,
                                            GetCompositorSurface(i));
                cam.Surface = texture;

                var renderTexture = _registeredCameras[i].Camera.targetTexture;
                var desc = renderTexture.descriptor;
                desc.msaaSamples = 1;
                _registeredCameras[i].RT = new RenderTexture(desc);
                _registeredCameras[i].RT.Create();
            }
        }

        private void RenderCameras(IntPtr iRenderParameters)
        {
            for (int surfaceId = 0; surfaceId < _registeredCameras.Count; ++surfaceId)
            {
                LockCompositorSurface(surfaceId);

                _registeredCameras[surfaceId].Camera.targetTexture.ResolveAntiAliasedSurface(_registeredCameras[surfaceId].RT);
                Graphics.CopyTexture(_registeredCameras[surfaceId].RT, 0, _registeredCameras[surfaceId].Surface, 0);

                UnlockCompositorSurface(surfaceId);

                vrCamera mvrCamera = (vrCamera)MVRNodesMapper.Instance.GetNode(_registeredCameras[surfaceId].Camera.gameObject);
                vrWarper mvrWarper = mvrCamera.GetWarper();
                vrViewport mvrVp = mvrCamera.GetViewport();

                vrVec3 pos = mvrCamera.GetPositionWorld();

                if (mvrWarper != null)
                {
                    pos = mvrCamera.GetPositionRelative(mvrWarper);
                }

                AddCameraRenderParameters(
                    iRenderParameters,
                    pos.x, pos.y, pos.z,
                    mvrVp.GetCornerOffsetTopLeftX(), mvrVp.GetCornerOffsetTopLeftY(),
                    mvrVp.GetCornerOffsetTopRightX(), mvrVp.GetCornerOffsetTopRightY(),
                    mvrVp.GetCornerOffsetBottomRightX(), mvrVp.GetCornerOffsetBottomRightY(),
                    mvrVp.GetCornerOffsetBottomLeftX(), mvrVp.GetCornerOffsetBottomLeftY(),
                    mvrVp.GetBlendingZoneLeft(), mvrVp.GetBlendingZoneRight(),
                    mvrVp.GetBlendingZoneBottom(), mvrVp.GetBlendingZoneTop()
                    );
            }
        }

        private void OnEndOfFrame()
        {
            IntPtr renderParameters = CreateRenderParameters();
            RenderCameras(renderParameters);
            RenderCompositor(renderParameters);
            PresentCompositor();
        }

        static void AddCamerasToCreationParameters(IntPtr iParameters, List<RegisteredCamera> iCameras)
        {
            for (int surfaceId = 0; surfaceId < iCameras.Count; ++surfaceId)
            {
                RegisteredCamera cameraInfo = iCameras[surfaceId];

                vrNode3D mvrNode = MVRNodesMapper.Instance.GetNode(cameraInfo.Camera.gameObject);

                int eye = 0; // left or mono by default
                if (cameraInfo.EyeMask == StereoTargetEyeMask.Right)
                {
                    eye = 1;
                }

                // Look for warping
                vrCamera mvrCam = (vrCamera)mvrNode;
                if (mvrCam != null)
                {
                    VRWarpingType warpingType = VRWarpingType.None;

                    string warperName = "";
                    string warpingCfg = "";
                    string warpingChannel = "";
                    float warpingScale = 1.0f;

                    vrWarper warper = mvrCam.GetWarper();

                    if (warper != null)
                    {
                        warperName = warper.GetName();

                        if (warper.GetWarpingType() == VRWarpingType.Vioso)
                        {
                            vrWarperVioso vioso = (vrWarperVioso)warper;

                            string cfg = vioso.GetConfigurationFile();
                            if (cfg.Length > 0)
                            {
                                warpingType = MiddleVR.VRWarpingType.Vioso;
                                warpingCfg = cfg;
                                warpingChannel = vioso.GetChannelName();
                            }
                        }
                        else if(warper.GetWarpingType() == VRWarpingType.ScalableDisplay)
                        {
                            vrWarperScalableDisplay sd = warper as vrWarperScalableDisplay;

                            string cfg = sd.GetConfigurationFile();

                            if (cfg.Length > 0)
                            {
                                warpingType = VRWarpingType.ScalableDisplay;
                                warpingCfg = cfg;
                                warpingScale = sd.GetUnitScale();
                            }
                        }
                    }

                    AddCamera(iParameters,
                              eye,
                              mvrNode.GetName(),
                              cameraInfo.Rect.x, cameraInfo.Rect.y, cameraInfo.Rect.width, cameraInfo.Rect.height,
                              warperName,
                              (int)warpingType,
                              warpingCfg,
                              warpingChannel,
                              warpingScale);
                }
            }

        }

#region Compositor Plugin Bindings

        public enum CompositorMode
        {
            Auto = 0,
            OpenGL = 1,
            Direct3D11 = 2,
        }

        private const string CompositorPluginName = "MiddleVR_UnityRendering";

        // synchronises compositor surface list

        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool IsRendererSupported();

        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        private static extern void ResetCompositor();

        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool WaitCompositor();

        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetCompositorSurface(int compositorSurfaceId);

        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr AddCamera(
            IntPtr parameters,
            int eye,
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            int vpX, int vpY, int vpW, int vpH,
            [MarshalAs(UnmanagedType.LPWStr)] string warperName,
            int warpintType,
            [MarshalAs(UnmanagedType.LPWStr)] string warpingCfgFile,
            [MarshalAs(UnmanagedType.LPWStr)] string warpingChannel,
            float warpingScale
            );

        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr AddCameraRenderParameters(
            IntPtr parameters,
            float x, float y, float z,
            int vpTLX, int vpTLY, int vpTRX, int vpTRY, int vpBRX, int vpBRY, int vpBLX, int vpBLY,
            int vpBZL, int vpBZR, int vpBZB, int vpBZT
            );

        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CreateRenderParameters();

        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CreateCompositorParameters(
            [MarshalAs(UnmanagedType.LPWStr)] string title,
            int x,
            int y,
            int width,
            int height,
            [MarshalAs(UnmanagedType.I1)] bool borders,
            [MarshalAs(UnmanagedType.I1)] bool alwaysOnTop,
            [MarshalAs(UnmanagedType.LPWStr)] string logFolder,
            [MarshalAs(UnmanagedType.I1)] bool vsync,
            CompositorMode mode,
            [MarshalAs(UnmanagedType.I1)] bool quadbuffer,
            [MarshalAs(UnmanagedType.I1)] bool nvswaplock,
            [MarshalAs(UnmanagedType.I1)] bool ndi);

        // Pass result of CreateCompositorParameters() as data
        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetCreateCompositorCallback();
        private static void CreateCompositor(
            string title,
            int x,
            int y,
            int width,
            int height,
            bool borders,
            bool alwaysOnTop,
            string logFolder,
            List<RegisteredCamera> cameras,
            bool vsync = false,
            CompositorMode mode = CompositorMode.Auto,
            bool quadbuffer = false,
            bool nvswaplock = false,
            bool ndi = false
            )
        {
            IntPtr parameters = CreateCompositorParameters(
                    title,
                    x,
                    y,
                    width,
                    height,
                    borders,
                    alwaysOnTop,
                    logFolder,
                    vsync,
                    mode,
                    quadbuffer,
                    nvswaplock,
                    ndi);

            AddCamerasToCreationParameters(parameters, cameras);

            var cmdBuf = new CommandBuffer();
            cmdBuf.IssuePluginEventAndData(
                GetCreateCompositorCallback(),
                0,
                parameters
                );
            Graphics.ExecuteCommandBuffer(cmdBuf);
        }

        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetDestroyCompositorCallback();
        private static void DestroyCompositor() => GL.IssuePluginEvent(GetDestroyCompositorCallback(), 0);

        // Pass result of GenerateCompositorSurfaceId() as event id
        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetLockCompositorSurfaceCallback();
        private static void LockCompositorSurface(int compositorSurfaceId) => GL.IssuePluginEvent(GetLockCompositorSurfaceCallback(), compositorSurfaceId);

        // Pass result of GenerateCompositorSurfaceId() as event id
        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetUnlockCompositorSurfaceCallback();
        private static void UnlockCompositorSurface(int compositorSurfaceId) => GL.IssuePluginEvent(GetUnlockCompositorSurfaceCallback(), compositorSurfaceId);

        // Pass result of CreateCompositorSurfaceStruct() as event id
        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetRenderCompositorCallback();
        private static void RenderCompositor(IntPtr parameters)
        {
            var cmdBuf = new CommandBuffer();
            cmdBuf.IssuePluginEventAndData(
                GetRenderCompositorCallback(),
                0,
                parameters
                );
            Graphics.ExecuteCommandBuffer(cmdBuf);
        }

        [DllImport(CompositorPluginName, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetPresentCompositorCallback();
        private static void PresentCompositor() => GL.IssuePluginEvent(GetPresentCompositorCallback(), 0);

#endregion
    }
}
