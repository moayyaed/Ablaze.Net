﻿//Ported from OpenTK, and excellent library.

#pragma warning disable 1591

namespace System.Graphics.EGL {
	[Flags]
	public enum RenderableFlags : int {
		OpenGLESBit = 1,
		OpenGLES2Bit = 4,
		OpenGLBit = 8,
		OpenVGBit = 2,
	}

	[Flags]
	public enum All : int {
		Version10 = 1,
		Version11 = 1,
		Version12 = 1,
		Version13 = 1,
		Version14 = 1,
		False = 0,
		True = 1,
		DontCare = -1,
		Success = 12288,
		NotInitialized = 12289,
		BadAccess = 12290,
		BadAlloc = 12291,
		BadAttribute = 12292,
		BadConfig = 12293,
		BadContext = 12294,
		BadCurrentSurface = 12295,
		BadDisplay = 12296,
		BadMatch = 12297,
		BadNativePixmap = 12298,
		BadNativeWindow = 12299,
		BadParameter = 12300,
		BadSurface = 12301,
		ContextLost = 12302,
		BufferSize = 12320,
		AlphaSize = 12321,
		BlueSize = 12322,
		GreenSize = 12323,
		RedSize = 12324,
		DepthSize = 12325,
		StencilSize = 12326,
		ConfigCaveat = 12327,
		ConfigId = 12328,
		Level = 12329,
		MaxPBufferHeight = 12330,
		MaxPBufferPixels = 12331,
		MaxPBufferWidth = 12332,
		NativeRenderable = 12333,
		NativeVisualId = 12334,
		NativeVisualType = 12335,
		PreservedResources = 12336,
		Samples = 12337,
		SampleBuffers = 12338,
		SurfaceType = 12339,
		TransparentType = 12340,
		TransparentBlueValue = 12341,
		TransparentGreenValue = 12342,
		TransparentRedValue = 12343,
		None = 12344,
		BindToTextureRgb = 12345,
		BindToTextureRgba = 12346,
		MinSwapInterval = 12347,
		MaxSwapInterval = 12348,
		LuminanceSize = 12349,
		AlphaMaskSize = 12350,
		ColorBufferType = 12351,
		RenderableType = 12352,
		MatchNativePixmap = 12353,
		Conformant = 12354,
		SlowConfig = 12368,
		NonConformantConfig = 12369,
		TransparentRgb = 12370,
		RgbBuffer = 12430,
		LuminanceBuffer = 12431,
		NoTexture = 12380,
		TextureRgb = 12381,
		TextureRgba = 12382,
		Texture2D = 12383,
		PBufferBit = 1,
		PixmapBit = 2,
		WindowBit = 4,
		VGColorSpaceLinearBit = 32,
		VGAlphaFormatPreBit = 64,
		MultiSampleResolveBoxBit = 512,
		SwapBehaviorPreservedBit = 1024,
		OpenGLESBit = 1,
		OpenVGBit = 2,
		OpenGLES2Bit = 4,
		OpenGLBit = 8,
		Vendor = 12371,
		Version = 12372,
		Extensions = 12373,
		ClientApis = 12429,
		Height = 12374,
		Width = 12375,
		LargestPBuffer = 12376,
		TextureFormat = 12416,
		TextureTarget = 12417,
		MipmapTexture = 12418,
		MipmapLevel = 12419,
		RenderBuffer = 12422,
		ColorSpace = 12423,
		AlphaFormat = 12424,
		HorizontalResolution = 12432,
		VerticalResolution = 12433,
		PixelAspectRatio = 12434,
		SwapBehavior = 12435,
		MultiSampleResolve = 12441,
		BackBuffer = 12420,
		SingleBuffer = 12421,
		ColorSpaceSrgb = 12425,
		ColorSpaceLinear = 12426,
		AlphaFormatNonPre = 12427,
		AlphaFormatPre = 12428,
		DisplayScaling = 10000,
		Unknown = -1,
		BufferPreserved = 12436,
		BufferDestroyed = 12437,
		OpenVGImage = 12438,
		ContextClientType = 12439,
		ContextClientVersion = 12440,
		MultiSampleResolveDefault = 12442,
		MultiSampleResolveBox = 12443,
		OpenGLESApi = 12448,
		OpenVGApi = 12449,
		OpenGLApi = 12450,
		Draw = 12377,
		Read = 12378,
		CoreNativeEngine = 12379,
	}
}

#pragma warning restore 1591