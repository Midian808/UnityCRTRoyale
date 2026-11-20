#ifndef _SHARED_OBJECTS_H
#define _SHARED_OBJECTS_H


















#include "../lib/helper-functions-and-macros.hlsl"
#include "../lib/derived-settings-and-constants.hlsl"
#include "../lib/bind-shader-params.hlsl"






































#define TEX_PREBLUR_VERT_WIDTH content_size.x
#define TEX_PREBLUR_VERT_HEIGHT content_size.y


sampler2D samplerPreblurVert;

#define TEX_PREBLUR_HORIZ_WIDTH content_size.x
#define TEX_PREBLUR_HORIZ_HEIGHT content_size.y


sampler2D samplerPreblurHoriz;


#define TEX_BEAMDIST_WIDTH num_beamdist_color_samples
#define TEX_BEAMDIST_HEIGHT num_beamdist_dist_samples
#define TEX_BEAMDIST_SIZE int2(TEX_BEAMDIST_WIDTH, TEX_BEAMDIST_HEIGHT)

sampler2D samplerBeamDist;


#define TEX_ELECTRONBEAMS_WIDTH content_size.x
#define TEX_ELECTRONBEAMS_HEIGHT content_size.y
#define TEX_ELECTRONBEAMS_SIZE int2(TEX_ELECTRONBEAMS_WIDTH, TEX_ELECTRONBEAMS_HEIGHT)

sampler2D samplerElectronBeams;




#define TEX_BEAMCONVERGENCE_WIDTH content_size.x
#define TEX_BEAMCONVERGENCE_HEIGHT content_size.y
#define TEX_BEAMCONVERGENCE_SIZE int2(TEX_BEAMCONVERGENCE_WIDTH, TEX_BEAMCONVERGENCE_HEIGHT)

sampler2D samplerBeamConvergence;






#define TEX_BLOOMAPPROXVERT_WIDTH content_size.x

#define TEX_BLOOMAPPROXVERT_HEIGHT int(content_size.y / bloomapprox_downsizing_factor)
#define TEX_BLOOMAPPROXVERT_SIZE int2(TEX_BLOOMAPPROXVERT_WIDTH, TEX_BLOOMAPPROXVERT_HEIGHT)

sampler2D samplerBloomApproxVert;






#define TEX_BLOOMAPPROXHORIZ_WIDTH int(content_size.x / bloomapprox_downsizing_factor)
#define TEX_BLOOMAPPROXHORIZ_HEIGHT TEX_BLOOMAPPROXVERT_HEIGHT
#define TEX_BLOOMAPPROXHORIZ_SIZE int2(TEX_BLOOMAPPROXHORIZ_WIDTH, TEX_BLOOMAPPROXHORIZ_HEIGHT)

sampler2D samplerBloomApproxHoriz;




#define TEX_BLURVERTICAL_WIDTH TEX_BLOOMAPPROXHORIZ_WIDTH
#define TEX_BLURVERTICAL_HEIGHT TEX_BLOOMAPPROXHORIZ_HEIGHT
#define TEX_BLURVERTICAL_SIZE int2(TEX_BLURVERTICAL_WIDTH, TEX_BLURVERTICAL_HEIGHT)

sampler2D samplerBlurVertical;




#define TEX_BLURHORIZONTAL_WIDTH TEX_BLOOMAPPROXHORIZ_WIDTH
#define TEX_BLURHORIZONTAL_HEIGHT TEX_BLOOMAPPROXHORIZ_HEIGHT
#define TEX_BLURHORIZONTAL_SIZE int2(TEX_BLURHORIZONTAL_WIDTH, TEX_BLURHORIZONTAL_HEIGHT)

sampler2D samplerBlurHorizontal;


#define TEX_DEINTERLACE_WIDTH content_size.x
#define TEX_DEINTERLACE_HEIGHT content_size.y
#define TEX_DEINTERLACE_SIZE int2(TEX_DEINTERLACE_WIDTH, TEX_DEINTERLACE_HEIGHT)
#if _DX9_ACTIVE == 0
	
	sampler2D samplerDeinterlace;
#else
	#define texDeinterlace texElectronBeams
	#define samplerDeinterlace samplerElectronBeams
#endif



#define TEX_FREEZEFRAME_WIDTH content_size.x
#define TEX_FREEZEFRAME_HEIGHT content_size.y
#define TEX_FREEZEFRAME_SIZE int2(TEX_FREEZEFRAME_WIDTH, TEX_FREEZEFRAME_HEIGHT)

sampler2D samplerFreezeFrame;



#define TEX_PHOSPHORMASK_WIDTH content_size.x
#define TEX_PHOSPHORMASK_HEIGHT content_size.y
#define TEX_PHOSPHORMASKL_SIZE int2(TEX_PHOSPHORMASK_WIDTH, TEX_PHOSPHORMASK_HEIGHT)

sampler2D samplerPhosphorMask;


#define TEX_MASKEDSCANLINES_WIDTH content_size.x
#define TEX_MASKEDSCANLINES_HEIGHT content_size.y
#define TEX_MASKEDSCANLINES_SIZE int2(TEX_MASKEDSCANLINES_WIDTH, TEX_MASKEDSCANLINES_HEIGHT)

#if _DX9_ACTIVE == 0
	
	sampler2D samplerMaskedScanlines;
#else
	#define texMaskedScanlines texBeamConvergence
	#define samplerMaskedScanlines samplerBeamConvergence
#endif


#define TEX_BRIGHTPASS_WIDTH content_size.x
#define TEX_BRIGHTPASS_HEIGHT content_size.y
#define TEX_BRIGHTPASS_SIZE int2(TEX_BRIGHTPASS_WIDTH, TEX_BRIGHTPASS_HEIGHT)

#if _DX9_ACTIVE == 0
	
	sampler2D samplerBrightpass;
#else
	#define texBrightpass texElectronBeams
	#define samplerBrightpass samplerElectronBeams
#endif




#define TEX_BLOOMVERTICAL_WIDTH content_size.x
#define TEX_BLOOMVERTICAL_HEIGHT content_size.y
#define TEX_BLOOMVERTICAL_SIZE int2(TEX_BLOOMVERTICAL_WIDTH, TEX_BLOOMVERTICAL_HEIGHT)

sampler2D samplerBloomVertical;




#define TEX_BLOOMHORIZONTAL_WIDTH content_size.x
#define TEX_BLOOMHORIZONTAL_HEIGHT content_size.y
#define TEX_BLOOMHORIZONTAL_SIZE int2(TEX_BLOOMHORIZONTAL_WIDTH, TEX_BLOOMHORIZONTAL_HEIGHT)

sampler2D samplerBloomHorizontal;


#define TEX_GEOMETRY_WIDTH content_size.x
#define TEX_GEOMETRY_HEIGHT content_size.y
#define TEX_GEOMETRY_SIZE int2(TEX_GEOMETRY_WIDTH, TEX_GEOMETRY_HEIGHT)

#if _DX9_ACTIVE == 0
	
	sampler2D samplerGeometry;
#else
	#define texGeometry texElectronBeams
	#define samplerGeometry samplerElectronBeams
#endif

sampler2D samplerBackBuffer;

#endif  // _SHARED_OBJECTS_H