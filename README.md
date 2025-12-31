# Shutter Based Temporal Post-Processing
Framerate-aware physical camera post-processing for Unity's Universal Render Pipeline.

Simulates a basic motion blur, depth of field and bloom using a custom temporal solution.

Unlike TAA and other common temporal solutions, SBTPP only requires the color & depth buffer and the current framerate to do its magic. No need for motion vectors, making it more friendly on your memory usage and bandwidth (like mobile tile-based GPUs).

Although this is a temporal solution, it plays well with URP's TAA and Unity's Spatial Temporal Post-Processing which can enhance SBTPP's quality on more powerful devices.

## Installation instructions
[Unity's official Package Manager installation instructions](https://docs.unity3d.com/Manual/upm-ui-giturl.html)

* Open the Package Manager Window, if it's not already open
* Open the **Add** (+) menu in the Package Manager's toolbar
* Select **Install package from git URL** from the install menu. A text box and an **install** button appear.
* Enter the following Git url in the text box:
```https://github.com/DesertHareStudios/Shutter-Based-Temporal-Post-Processing.git#v0.3.0```
* Select **Install**

## Requirements
This package is compatible only with **Unity 6.3** (The current LTS at the time) and the **Universal Render Pipeline**.

> [!NOTE] 
> SBTPP has only been tested on **DirectX 12** on **Windows**, **Vulkan** on **Android** and **WebGPU**. While we believe other platforms shouldn't have any issue, keep in mind that this is still an experimental package.

## Limitations
### Framerate-awareness
To prevent undesired ghosting and excesive motion blur. SBTPP tests your current `Physical Camera`'s `Shutter Speed` against your current framerate (A predicted `Time.unscaledDeltaTime`). Which can give great results at high framerates but SBTPP essentially "turns off" at low framerates.

To ensure the best quality, make sure you game/app is properly optimized and play around with different `Shutter Speed` values.

### Motion Blur without Motion Vectors
The motion blur effect is achieved with simple alpha blending of the current frame against the history buffer using **Interleaved Gradient Noise** to dither the calculated intensity from the `Shutter Speed` and current framerate to hide any "step" artifacts. The Depth of Field and Bloom effects slighty hide the pixel artifacts but can still be seen on some cases.

## Workflow
* Locate your current `Universal Renderer` asset. (You can find it easier by checking your `Universal Render Pipeline` asset)
* Add the `Shutter Based Temporal Post-Processing` render feature.
* Add the `Shutter Camera` component to any `Camera` you wish to enable SBTPP for.

Using `Volume Profiles`, you can change the physical properties of your camera. If you camera has `usePhysicalProperties` set to `true`, SBTPP will use those settings instead unless explicitly specified by the `Physical Camera` volume.

### Physical Camera

#### Camera Body
Settings for your camera's light sensor

* Shutter Angle
  * Calculates the appropiate shutter speed according to the current VSync Count, `Application.targetFrameRate` and screen refresh rate.
  * Affects:
    * Temporal Accumulation Factor
    * Motion Blur

#### Lens
Controls how the camera focuses light.

* Focus Distance
  * The distance from the camera to your subject or point of interest
  * Affects:
    * Depth of Field
* Aperture
  * The f-stop number, the physical size of the lens aperture.
  * Affects:
    * Depth of Field
    * Bloom

#### Aperture Shape
Controls the shape of the depth of field and bokeh.

* Blades
  * The ammount of blades of the lens.
  * Affects:
    * Depth of Field
* Min Blade Curvature
  * The aperture value at which the blades are fully curved.
  * Affects:
    * Depth of Field
* Max Blade Curvature
  * The aperture value at which the blades are completely visible.
  * Affects:
    * Depth of Field

### Settings
* Depth of Field Resolution
  * The resolution scale of the blur texture and circle of confusion prepass
  * Affects:
    * Depth of Field
    * Bloom
* Circle of Confussion Target
  * Which render texture to use for the depth of field blur prepass
    * **Active Color**: Uses the current camera's color buffer (More stable, but lower quality blur)
    * **History Buffer**: Uses the SBTPP accumulation buffer (Better blur, but prone to ghosting)