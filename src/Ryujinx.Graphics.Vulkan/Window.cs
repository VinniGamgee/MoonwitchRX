using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Vulkan.Effects;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using VkFormat = Silk.NET.Vulkan.Format;
using System.Linq;

namespace Ryujinx.Graphics.Vulkan
{
    class Window : WindowBase, IDisposable
    {
        private const int SurfaceWidth = 1280;
        private const int SurfaceHeight = 720;

        private readonly VulkanRenderer _gd;
        private readonly PhysicalDevice _physicalDevice;
        private readonly Device _device;
        private SurfaceKHR _surface;
        private SwapchainKHR _swapchain;

        private Image[] _swapchainImages;
        private TextureView[] _swapchainImageViews;

        private Semaphore[] _imageAvailableSemaphores;
        private Semaphore[] _renderFinishedSemaphores;

        private int _frameIndex;

        private int _width;
        private int _height;
        private VSyncMode _vSyncMode;
        private bool _swapchainIsDirty;
        private VkFormat _format;
        private AntiAliasing _currentAntiAliasing;
        private bool _updateEffect;
        private IPostProcessingEffect _effect;
        private IScalingFilter _scalingFilter;
        private bool _isLinear;
        private float _scalingFilterLevel;
        private bool _updateScalingFilter;
        private ScalingFilter _currentScalingFilter;
        private bool _colorSpacePassthroughEnabled;

        // Gateway for all vk*Surface* queries
        private volatile bool _allowSurfaceQueries = true;

        public unsafe Window(VulkanRenderer gd, SurfaceKHR surface, PhysicalDevice physicalDevice, Device device)
        {
            _gd = gd;
            _physicalDevice = physicalDevice;
            _device = device;
            _surface = surface;

            if (_gd.PresentAllowed && _surface.Handle != 0)
            {
                CreateSwapchain();
            }
            else
            {
                _swapchainIsDirty = true;
            }
        }

        public void SetSurfaceQueryAllowed(bool allowed) => _allowSurfaceQueries = allowed;
        private bool CanQuerySurface() => _allowSurfaceQueries && _gd.PresentAllowed && _surface.Handle != 0;

        private unsafe bool TryGetSurfaceCapabilities(out SurfaceCapabilitiesKHR caps)
        {
            caps = default;
            if (!CanQuerySurface()) return false;
            var res = _gd.SurfaceApi.GetPhysicalDeviceSurfaceCapabilities(_physicalDevice, _surface, out caps);
            return res == Result.Success;
        }

        private unsafe bool TryGetSurfaceFormats(out SurfaceFormatKHR[] formats)
        {
            formats = Array.Empty<SurfaceFormatKHR>();
            if (!CanQuerySurface()) return false;

            uint count = 0;
            var res = _gd.SurfaceApi.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, &count, null);
            if (res != Result.Success || count == 0) return false;

            formats = new SurfaceFormatKHR[count];
            fixed (SurfaceFormatKHR* p = formats)
            {
                if (_gd.SurfaceApi.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, &count, p) != Result.Success)
                    return false;
            }
            return true;
        }

        private unsafe bool TryGetPresentModes(out PresentModeKHR[] modes)
        {
            modes = Array.Empty<PresentModeKHR>();
            if (!CanQuerySurface()) return false;

            uint count = 0;
            var res = _gd.SurfaceApi.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, &count, null);
            if (res != Result.Success || count == 0) return false;

            modes = new PresentModeKHR[count];
            fixed (PresentModeKHR* p = modes)
            {
                if (_gd.SurfaceApi.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, &count, p) != Result.Success)
                    return false;
            }
            return true;
        }

        private void RecreateSwapchain()
        {
            if (!_gd.PresentAllowed || _surface.Handle == 0 || !CanQuerySurface())
            {
                _swapchainIsDirty = true;
                return;
            }

            lock (_gd.SurfaceLock)
            {
                var oldSwapchain = _swapchain;
                _swapchainIsDirty = false;

                if (_swapchainImageViews != null)
                {
                    for (int i = 0; i < _swapchainImageViews.Length; i++)
                    {
                        _swapchainImageViews[i]?.Dispose();
                    }
                }

                _gd.Api.DeviceWaitIdle(_device);

                unsafe
                {
                    if (_imageAvailableSemaphores != null)
                    {
                        for (int i = 0; i < _imageAvailableSemaphores.Length; i++)
                        {
                            if (_imageAvailableSemaphores[i].Handle != 0)
                            {
                                _gd.Api.DestroySemaphore(_device, _imageAvailableSemaphores[i], null);
                            }
                        }
                    }

                    if (_renderFinishedSemaphores != null)
                    {
                        for (int i = 0; i < _renderFinishedSemaphores.Length; i++)
                        {
                            if (_renderFinishedSemaphores[i].Handle != 0)
                            {
                                _gd.Api.DestroySemaphore(_device, _renderFinishedSemaphores[i], null);
                            }
                        }
                    }
                }

                if (oldSwapchain.Handle != 0)
                {
                    _gd.SwapchainApi.DestroySwapchain(_device, oldSwapchain, Span<AllocationCallbacks>.Empty);
                }

                CreateSwapchain();
            }
        }

        internal void SetSurface(SurfaceKHR surface)
        {
            lock (_gd.SurfaceLock)
            {
                _surface = surface;

                if (!_gd.PresentAllowed || _surface.Handle == 0)
                {
                    _swapchainIsDirty = true;
                    return;
                }

                SetSurfaceQueryAllowed(true);
                RecreateSwapchain();
            }
        }

        private unsafe void CreateSwapchain()
        {
            if (!_gd.PresentAllowed || _surface.Handle == 0 || !CanQuerySurface())
            {
                _swapchainIsDirty = true;
                return;
            }

            lock (_gd.SurfaceLock)
            {
                if (!TryGetSurfaceCapabilities(out var capabilities))
                {
                    _swapchainIsDirty = true;
                    return;
                }

                if (!TryGetSurfaceFormats(out var surfaceFormats))
                {
                    _swapchainIsDirty = true;
                    return;
                }

                if (!TryGetPresentModes(out var presentModes))
                {
                    _swapchainIsDirty = true;
                    return;
                }

                uint imageCount = capabilities.MinImageCount + 1;
                if (capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount)
                {
                    imageCount = capabilities.MaxImageCount;
                }

                var surfaceFormat = ChooseSwapSurfaceFormat(surfaceFormats, _colorSpacePassthroughEnabled);
                var extent = ChooseSwapExtent(capabilities);

                // Guard against 0x0 extent immediately after resume
                if (extent.Width == 0 || extent.Height == 0)
                {
                    _swapchainIsDirty = true;
                    return;
                }

                _width = (int)extent.Width;
                _height = (int)extent.Height;
                _format = surfaceFormat.Format;

                var oldSwapchain = _swapchain;

                CurrentTransform = capabilities.CurrentTransform;

                var usage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferDstBit;
                if (!PlatformInfo.IsBionic)
                {
                    usage |= ImageUsageFlags.StorageBit; // Only desktop allows storage for swapchain
                }

                // On Android: Identity; otherwise, the CurrentTransform recommended by the driver
                var preTransform = PlatformInfo.IsBionic
                    ? SurfaceTransformFlagsKHR.IdentityBitKhr
                    : capabilities.CurrentTransform;

                var swapchainCreateInfo = new SwapchainCreateInfoKHR
                {
                    SType = StructureType.SwapchainCreateInfoKhr,
                    Surface = _surface,
                    MinImageCount = imageCount,
                    ImageFormat = surfaceFormat.Format,
                    ImageColorSpace = surfaceFormat.ColorSpace,
                    ImageExtent = extent,
                    ImageUsage = usage,
                    ImageSharingMode = SharingMode.Exclusive,
                    ImageArrayLayers = 1,
                    PreTransform = preTransform,
                    CompositeAlpha = ChooseCompositeAlpha(capabilities.SupportedCompositeAlpha),
                    PresentMode = ChooseSwapPresentMode(presentModes, _vSyncMode),
                    Clipped = true,
                };

                var textureCreateInfo = new TextureCreateInfo(
                    _width,
                    _height,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    FormatTable.GetFormat(surfaceFormat.Format),
                    DepthStencilMode.Depth,
                    Target.Texture2D,
                    SwizzleComponent.Red,
                    SwizzleComponent.Green,
                    SwizzleComponent.Blue,
                    SwizzleComponent.Alpha);

                _gd.SwapchainApi.CreateSwapchain(_device, in swapchainCreateInfo, null, out _swapchain).ThrowOnError();

                _gd.SwapchainApi.GetSwapchainImages(_device, _swapchain, &imageCount, null);

                _swapchainImages = new Image[imageCount];

                fixed (Image* pSwapchainImages = _swapchainImages)
                {
                    _gd.SwapchainApi.GetSwapchainImages(_device, _swapchain, &imageCount, pSwapchainImages);
                }

                _swapchainImageViews = new TextureView[imageCount];

                for (int i = 0; i < _swapchainImageViews.Length; i++)
                {
                    _swapchainImageViews[i] = CreateSwapchainImageView(_swapchainImages[i], surfaceFormat.Format, textureCreateInfo);
                }

                var semaphoreCreateInfo = new SemaphoreCreateInfo
                {
                    SType = StructureType.SemaphoreCreateInfo,
                };

                _imageAvailableSemaphores = new Semaphore[imageCount];

                for (int i = 0; i < _imageAvailableSemaphores.Length; i++)
                {
                    _gd.Api.CreateSemaphore(_device, in semaphoreCreateInfo, null, out _imageAvailableSemaphores[i]).ThrowOnError();
                }

                _renderFinishedSemaphores = new Semaphore[imageCount];

                for (int i = 0; i < _renderFinishedSemaphores.Length; i++)
                {
                    _gd.Api.CreateSemaphore(_device, in semaphoreCreateInfo, null, out _renderFinishedSemaphores[i]).ThrowOnError();
                }
            }
        }

        private unsafe TextureView CreateSwapchainImageView(Image swapchainImage, VkFormat format, TextureCreateInfo info)
        {
            var componentMapping = new ComponentMapping(
                ComponentSwizzle.R,
                ComponentSwizzle.G,
                ComponentSwizzle.B,
                ComponentSwizzle.A);

            var aspectFlags = ImageAspectFlags.ColorBit;

            var subresourceRange = new ImageSubresourceRange(aspectFlags, 0, 1, 0, 1);

            var imageCreateInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = swapchainImage,
                ViewType = ImageViewType.Type2D,
                Format = format,
                Components = componentMapping,
                SubresourceRange = subresourceRange,
            };

            _gd.Api.CreateImageView(_device, in imageCreateInfo, null, out var imageView).ThrowOnError();

            return new TextureView(_gd, _device, new DisposableImageView(_gd.Api, _device, imageView), info, format);
        }

        private static SurfaceFormatKHR ChooseSwapSurfaceFormat(SurfaceFormatKHR[] availableFormats, bool colorSpacePassthroughEnabled)
        {
            if (availableFormats == null || availableFormats.Length == 0)
            {
                return new SurfaceFormatKHR(VkFormat.B8G8R8A8Unorm, (ColorSpaceKHR)0);
            }

            if (availableFormats.Length == 1 && availableFormats[0].Format == VkFormat.Undefined)
            {
                return new SurfaceFormatKHR(VkFormat.B8G8R8A8Unorm, availableFormats[0].ColorSpace);
            }

            foreach (var f in availableFormats)
            {
                if (f.Format == VkFormat.B8G8R8A8Unorm)
                {
                    return f;
                }
            }

            return availableFormats[0];
        }

        private static CompositeAlphaFlagsKHR ChooseCompositeAlpha(CompositeAlphaFlagsKHR supportedFlags)
        {
            if (supportedFlags.HasFlag(CompositeAlphaFlagsKHR.OpaqueBitKhr))
            {
                return CompositeAlphaFlagsKHR.OpaqueBitKhr;
            }
            else
            {
                return supportedFlags.HasFlag(CompositeAlphaFlagsKHR.PreMultipliedBitKhr)
                    ? CompositeAlphaFlagsKHR.PreMultipliedBitKhr
                    : CompositeAlphaFlagsKHR.InheritBitKhr;
            }
        }

        private static PresentModeKHR ChooseSwapPresentMode(PresentModeKHR[] availablePresentModes, VSyncMode vSyncMode)
        {
            if (vSyncMode == VSyncMode.Unbounded && availablePresentModes.Contains(PresentModeKHR.ImmediateKhr))
            {
                return PresentModeKHR.ImmediateKhr;
            }
            else
            {
                return availablePresentModes.Contains(PresentModeKHR.MailboxKhr) ? PresentModeKHR.MailboxKhr : PresentModeKHR.FifoKhr;
            }
        }

        public static Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
        {
            if (capabilities.CurrentExtent.Width != uint.MaxValue)
            {
                return capabilities.CurrentExtent;
            }

            uint width = Math.Max(capabilities.MinImageExtent.Width, Math.Min(capabilities.MaxImageExtent.Width, SurfaceWidth));
            uint height = Math.Max(capabilities.MinImageExtent.Height, Math.Min(capabilities.MaxImageExtent.Height, SurfaceHeight));

            return new Extent2D(width, height);
        }

        public unsafe override void Present(ITexture texture, ImageCrop crop, Action swapBuffersCallback)
        {
            // If Surface is already new, but queries are still blocked → unblock them.
            if (!_allowSurfaceQueries && _surface.Handle != 0)
            {
                _allowSurfaceQueries = true;
            }

            if (!_gd.PresentAllowed || _surface.Handle == 0)
            {
                swapBuffersCallback?.Invoke();
                return;
            }

            // If the size is not yet available, rebuild the swapchain later
            if (_width <= 0 || _height <= 0)
            {
                RecreateSwapchain();
                swapBuffersCallback?.Invoke();
                return;
            }

            // Lazy Initialization/Recovery
            if (_swapchain.Handle == 0 || _imageAvailableSemaphores == null || _renderFinishedSemaphores == null)
            {
                try { CreateSwapchain(); } catch { /* try again next frame */ }
                if (_swapchain.Handle == 0 || _imageAvailableSemaphores == null || _renderFinishedSemaphores == null)
                {
                    swapBuffersCallback?.Invoke();
                    return;
                }
            }

            _gd.PipelineInternal.AutoFlush.Present();

            uint nextImage = 0;
            int semaphoreIndex = _frameIndex++ % _imageAvailableSemaphores.Length;

            while (true)
            {
                var acquireResult = _gd.SwapchainApi.AcquireNextImage(
                    _device,
                    _swapchain,
                    ulong.MaxValue,
                    _imageAvailableSemaphores[semaphoreIndex],
                    new Fence(),
                    ref nextImage);

                if (acquireResult == Result.ErrorOutOfDateKhr ||
                    acquireResult == Result.SuboptimalKhr ||
                    _swapchainIsDirty)
                {
                    RecreateSwapchain();

                    if (_swapchain.Handle == 0 || _imageAvailableSemaphores == null)
                    {
                        swapBuffersCallback?.Invoke();
                        return;
                    }

                    semaphoreIndex = (_frameIndex - 1) % _imageAvailableSemaphores.Length;
                }
                else if (acquireResult == Result.ErrorSurfaceLostKhr)
                {
                    // Do not recreate in the background immediately – release and return
                    _gd.ReleaseSurface();
                    swapBuffersCallback?.Invoke();
                    return;
                }
                else
                {
                    acquireResult.ThrowOnError();
                    break;
                }
            }

            var swapchainImage = _swapchainImages[nextImage];

            _gd.FlushAllCommands();

            var cbs = _gd.CommandBufferPool.Rent();

            // --- Set layout/stages correctly depending on the path ---
            bool allowStorageDst = !PlatformInfo.IsBionic; // Android: kein Storage auf Swapchain
            bool useComputeDst = allowStorageDst && _scalingFilter != null;

            if (useComputeDst)
            {
                // Compute writes to the swapchain image → General + ShaderWrite
                Transition(
                    cbs.CommandBuffer,
                    swapchainImage,
                    PipelineStageFlags.TopOfPipeBit,
                    PipelineStageFlags.ComputeShaderBit,
                    0,
                    AccessFlags.ShaderWriteBit,
                    ImageLayout.Undefined,
                    ImageLayout.General);
            }
            else
            {
                // Render pass writes to the swapchain image → ColorAttachmentOptimal
                Transition(
                    cbs.CommandBuffer,
                    swapchainImage,
                    PipelineStageFlags.TopOfPipeBit,
                    PipelineStageFlags.ColorAttachmentOutputBit,
                    0,
                    AccessFlags.ColorAttachmentWriteBit,
                    ImageLayout.Undefined,
                    ImageLayout.ColorAttachmentOptimal);
            }

            var view = (TextureView)texture;

            UpdateEffect();

            if (_effect != null)
            {
                view = _effect.Run(view, cbs, _width, _height);
            }

            int srcX0, srcX1, srcY0, srcY1;

            if (crop.Left == 0 && crop.Right == 0)
            {
                srcX0 = 0;
                srcX1 = view.Width;
            }
            else
            {
                srcX0 = crop.Left;
                srcX1 = crop.Right;
            }

            if (crop.Top == 0 && crop.Bottom == 0)
            {
                srcY0 = 0;
                srcY1 = view.Height;
            }
            else
            {
                srcY0 = crop.Top;
                srcY1 = crop.Bottom;
            }

            if (ScreenCaptureRequested)
            {
                if (_effect != null)
                {
                    var emptySems = Array.Empty<Silk.NET.Vulkan.Semaphore>();
                    var waitStagesCO = new PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit };
                    _gd.CommandBufferPool.Return(
                        cbs,
                        emptySems,
                        waitStagesCO,
                        emptySems);
                    _gd.FlushAllCommands();
                    cbs.GetFence().Wait();
                    cbs = _gd.CommandBufferPool.Rent();
                }

                CaptureFrame(view, srcX0, srcY0, srcX1 - srcX0, srcY1 - srcY0, view.Info.Format.IsBgr(), crop.FlipX, crop.FlipY);
                ScreenCaptureRequested = false;
            }

            float ratioX = crop.IsStretched ? 1.0f : MathF.Min(1.0f, _height * crop.AspectRatioX / (_width * crop.AspectRatioY));
            float ratioY = crop.IsStretched ? 1.0f : MathF.Min(1.0f, _width * crop.AspectRatioY / (_height * crop.AspectRatioX));

            int dstWidth = (int)(_width * ratioX);
            int dstHeight = (int)(_height * ratioY);

            int dstPaddingX = (_width - dstWidth) / 2;
            int dstPaddingY = (_height - dstHeight) / 2;

            int dstX0 = crop.FlipX ? _width - dstPaddingX : dstPaddingX;
            int dstX1 = crop.FlipX ? dstPaddingX : _width - dstPaddingX;

            int dstY0 = crop.FlipY ? dstPaddingY : _height - dstPaddingY;
            int dstY1 = crop.FlipY ? _height - dstPaddingY : dstPaddingY;

            if (_scalingFilter != null && useComputeDst)
            {
                _scalingFilter!.Run(
                    view,
                    cbs,
                    _swapchainImageViews[nextImage].GetImageViewForAttachment(),
                    _format,
                    _width,
                    _height,
                    new Extents2D(srcX0, srcY0, srcX1, srcY1),
                    new Extents2D(dstX0, dstY0, dstX1, dstY1)
                );
            }
            else
            {
                _gd.HelperShader.BlitColor(
                    _gd,
                    cbs,
                    view,
                    _swapchainImageViews[nextImage],
                    new Extents2D(srcX0, srcY0, srcX1, srcY1),
                    new Extents2D(dstX0, dstY1, dstX1, dstY0),
                    _isLinear,
                    true);
            }

            // Transition to Present – Stages/Access depending on the previous path
            if (useComputeDst)
            {
                Transition(
                    cbs.CommandBuffer,
                    swapchainImage,
                    PipelineStageFlags.ComputeShaderBit,
                    PipelineStageFlags.BottomOfPipeBit,
                    AccessFlags.ShaderWriteBit,
                    0,
                    ImageLayout.General,
                    ImageLayout.PresentSrcKhr);
            }
            else
            {
                Transition(
                    cbs.CommandBuffer,
                    swapchainImage,
                    PipelineStageFlags.ColorAttachmentOutputBit,
                    PipelineStageFlags.BottomOfPipeBit,
                    AccessFlags.ColorAttachmentWriteBit,
                    0,
                    ImageLayout.ColorAttachmentOptimal,
                    ImageLayout.PresentSrcKhr);
            }

            var waitSems = new Silk.NET.Vulkan.Semaphore[] { _imageAvailableSemaphores[semaphoreIndex] };
            var waitStages = new PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit }; // Important on Android
            var signalSems = new Silk.NET.Vulkan.Semaphore[] { _renderFinishedSemaphores[semaphoreIndex] };
            _gd.CommandBufferPool.Return(cbs, waitSems, waitStages, signalSems);

            PresentOne(_gd, _renderFinishedSemaphores[semaphoreIndex], _swapchain, nextImage);

            swapBuffersCallback?.Invoke();
        }

        private static unsafe void PresentOne(
            VulkanRenderer gd,
            Silk.NET.Vulkan.Semaphore signal,
            SwapchainKHR swapchain,
            uint imageIndex)
        {
            Silk.NET.Vulkan.Semaphore* pWait = stackalloc Silk.NET.Vulkan.Semaphore[1];
            SwapchainKHR* pSwap = stackalloc SwapchainKHR[1];
            uint* pImageIndex = stackalloc uint[1];

            pWait[0] = signal;
            pSwap[0] = swapchain;
            pImageIndex[0] = imageIndex;

            var presentInfo = new PresentInfoKHR
            {
                SType = StructureType.PresentInfoKhr,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = pWait,
                SwapchainCount = 1,
                PSwapchains = pSwap,
                PImageIndices = pImageIndex,
                PResults = null
            };

            lock (gd.QueueLock)
            {
                gd.SwapchainApi.QueuePresent(gd.Queue, in presentInfo);
            }
        }

        public override void SetAntiAliasing(AntiAliasing effect)
        {
            if (_currentAntiAliasing == effect && _effect != null)
            {
                return;
            }

            _currentAntiAliasing = effect;

            _updateEffect = true;
        }

        public override void SetScalingFilter(ScalingFilter type)
        {
            if (_currentScalingFilter == type && _effect != null)
            {
                return;
            }

            _currentScalingFilter = type;

            _updateScalingFilter = true;
        }

        public override void SetColorSpacePassthrough(bool colorSpacePassthroughEnabled)
        {
            _colorSpacePassthroughEnabled = colorSpacePassthroughEnabled;
            _swapchainIsDirty = true;
        }

        private void UpdateEffect()
        {
            if (_updateEffect)
            {
                _updateEffect = false;

                switch (_currentAntiAliasing)
                {
                    case AntiAliasing.Fxaa:
                        _effect?.Dispose();
                        _effect = new FxaaPostProcessingEffect(_gd, _device);
                        break;
                    case AntiAliasing.None:
                        _effect?.Dispose();
                        _effect = null;
                        break;
                    case AntiAliasing.SmaaLow:
                    case AntiAliasing.SmaaMedium:
                    case AntiAliasing.SmaaHigh:
                    case AntiAliasing.SmaaUltra:
                        var quality = _currentAntiAliasing - AntiAliasing.SmaaLow;
                        if (_effect is SmaaPostProcessingEffect smaa)
                        {
                            smaa.Quality = quality;
                        }
                        else
                        {
                            _effect?.Dispose();
                            _effect = new SmaaPostProcessingEffect(_gd, _device, quality);
                        }
                        break;
                }
            }

            if (_updateScalingFilter)
            {
                _updateScalingFilter = false;

                switch (_currentScalingFilter)
                {
                    case ScalingFilter.Bilinear:
                    case ScalingFilter.Nearest:
                        _scalingFilter?.Dispose();
                        _scalingFilter = null;
                        _isLinear = _currentScalingFilter == ScalingFilter.Bilinear;
                        break;
                    case ScalingFilter.Fsr:
                        if (_scalingFilter is not FsrScalingFilter)
                        {
                            _scalingFilter?.Dispose();
                            _scalingFilter = new FsrScalingFilter(_gd, _device);
                        }

                        _scalingFilter.Level = _scalingFilterLevel;
                        break;
                    case ScalingFilter.Area:
                        if (_scalingFilter is not AreaScalingFilter)
                        {
                            _scalingFilter?.Dispose();
                            _scalingFilter = new AreaScalingFilter(_gd, _device);
                        }
                        break;
                }
            }
        }

        public override void SetScalingFilterLevel(float level)
        {
            _scalingFilterLevel = level;
            _updateScalingFilter = true;
        }

        private unsafe void Transition(
            CommandBuffer commandBuffer,
            Image image,
            PipelineStageFlags srcStage,
            PipelineStageFlags dstStage,
            AccessFlags srcAccess,
            AccessFlags dstAccess,
            ImageLayout srcLayout,
            ImageLayout dstLayout)
        {
            var subresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1);

            var barrier = new ImageMemoryBarrier
            {
                SType = StructureType.ImageMemoryBarrier,
                SrcAccessMask = srcAccess,
                DstAccessMask = dstAccess,
                OldLayout = srcLayout,
                NewLayout = dstLayout,
                SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
                DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
                Image = image,
                SubresourceRange = subresourceRange,
            };

            _gd.Api.CmdPipelineBarrier(
                commandBuffer,
                srcStage,
                dstStage,
                0,
                0,
                null,
                0,
                null,
                1,
                in barrier);
        }

        private void CaptureFrame(TextureView texture, int x, int y, int width, int height, bool isBgra, bool flipX, bool flipY)
        {
            byte[] bitmap = texture.GetData(x, y, width, height);

            _gd.OnScreenCaptured(new ScreenCaptureImageInfo(width, height, isBgra, bitmap, flipX, flipY));
        }

        public override void SetSize(int width, int height)
        {
            // We don't need to use width and height as we can get the size from the surface.
            _swapchainIsDirty = true;

            // After resuming, ensure that surface queries are allowed again,
            // in case OnSurfaceLost() had previously closed the gate.
            if (_surface.Handle != 0)
            {
                SetSurfaceQueryAllowed(true);
            }
        }

        public override void ChangeVSyncMode(VSyncMode vSyncMode)
        {
            _vSyncMode = vSyncMode;
            // Present mode may change, so mark the swapchain for recreation
            _swapchainIsDirty = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_gd.SurfaceLock)
                {
                    unsafe
                    {
                        if (_swapchainImageViews != null)
                        {
                            for (int i = 0; i < _swapchainImageViews.Length; i++)
                            {
                                _swapchainImageViews[i]?.Dispose();
                            }
                        }

                        if (_imageAvailableSemaphores != null)
                        {
                            for (int i = 0; i < _imageAvailableSemaphores.Length; i++)
                            {
                                if (_imageAvailableSemaphores[i].Handle != 0)
                                {
                                    _gd.Api.DestroySemaphore(_device, _imageAvailableSemaphores[i], null);
                                }
                            }
                        }

                        if (_renderFinishedSemaphores != null)
                        {
                            for (int i = 0; i < _renderFinishedSemaphores.Length; i++)
                            {
                                if (_renderFinishedSemaphores[i].Handle != 0)
                                {
                                    _gd.Api.DestroySemaphore(_device, _renderFinishedSemaphores[i], null);
                                }
                            }
                        }

                        if (_swapchain.Handle != 0)
                        {
                            _gd.SwapchainApi.DestroySwapchain(_device, _swapchain, null);
                        }
                    }
                }

                _effect?.Dispose();
                _scalingFilter?.Dispose();
            }
        }

        public void OnSurfaceLost()
        {
            lock (_gd.SurfaceLock)
            {
                // Thorough cleanup operation so that nothing "old" remains after the resume
                _swapchainIsDirty = true;
                SetSurfaceQueryAllowed(false);

                _gd.Api.DeviceWaitIdle(_device);

                unsafe
                {
                    if (_imageAvailableSemaphores != null)
                    {
                        for (int i = 0; i < _imageAvailableSemaphores.Length; i++)
                        {
                            if (_imageAvailableSemaphores[i].Handle != 0)
                            {
                                _gd.Api.DestroySemaphore(_device, _imageAvailableSemaphores[i], null);
                            }
                        }
                        _imageAvailableSemaphores = null;
                    }

                    if (_renderFinishedSemaphores != null)
                    {
                        for (int i = 0; i < _renderFinishedSemaphores.Length; i++)
                        {
                            if (_renderFinishedSemaphores[i].Handle != 0)
                            {
                                _gd.Api.DestroySemaphore(_device, _renderFinishedSemaphores[i], null);
                            }
                        }
                        _renderFinishedSemaphores = null;
                    }
                }

                if (_swapchainImageViews != null)
                {
                    for (int i = 0; i < _swapchainImageViews.Length; i++)
                    {
                        _swapchainImageViews[i]?.Dispose();
                    }
                    _swapchainImageViews = null;
                }

                if (_swapchain.Handle != 0)
                {
                    _gd.SwapchainApi.DestroySwapchain(_device, _swapchain, Span<AllocationCallbacks>.Empty);
                    _swapchain = default;
                }

                _surface = new SurfaceKHR(0);
                _width = _height = 0; // Forces a later, clean recreate path
            }
        }

        public override void Dispose()
        {
            Dispose(true);
        }
    }
}
