# Shutter Based Temporal Post-Processing
Framerate-aware physical camera post-processing for Unity's Universal Render Pipeline.

Simulates a basic motion blur, depth of field, bloom, lens flares, exposure and some anti-aliasing using a custom temporal solution.

Unlike TAA and other common temporal solutions, SBTPP only requires the color buffer and the current framerate to do its magic. No need for motion vectors nor depth buffer, making it suitable for mobile tiled-based GPUs.

## Installation instructions
[Unity's official Package Manager installation instructions](https://docs.unity3d.com/Manual/upm-ui-giturl.html)

* Open the Package Manager Window, if it's not already open
* Open the **Add** (+) menu in the Package Manager's toolbar
* Select **Install package from git URL** from the install menu. A text box and an **install** button appear.
* Enter the following Git url in the text box:
```https://github.com/DesertHareStudios/Shutter-Based-Temporal-Post-Processing.git#v0.1.0```
* Select **Install**

## Requirements
This package is compatible only with **Unity 6/6.1** and the **Universal Render Pipeline**.

> [!NOTE] 
> SBTPP has only been tested on **DirectX 12** on **Windows**, **Vulkan** on **Android** and **WebGPU**. While we believe other platforms shouldn't have any issue, keep in mind that this is still an experimental package.

## Limitations
### Framerate-awareness
To prevent undesired ghosting and excesive motion blur. SBTPP tests your current `Physical Camera`'s `Shutter Speed` against your current framerate (A predicted `Time.unscaledDeltaTime`). Which can give great results at high framerates but SBTPP essentially "turns off" at low framerates.

To ensure the best quality, make sure you game/app is properly optimized and play around with different `Shutter Speed` values.

### Depth of Field without Depth Buffer
The depth of field effect is simulated through camera jitter and temporal accumulation. While this works in most cases, sometimes you can notice excessive jittering on certain combinations of `Aperture`, `Shutter Speed`, `Focus Distance` and framerate.

You can easily "disable" Depth of Field with the `Additional Settings` volume by lowering the `Max Aperture Jitter Allowed` value.

## Workflow
* Locate your current `Universal Renderer` asset. (You can find it easier by checking your `Universal Render Pipeline` asset)
* Add the `Shutter Based Temporal Post-Processing` render feature.
* Add the `Shutter Camera` component to any `Camera` you wish to enable SBTPP for.

> [!NOTE]
> Any `Camera` that doesn't have the `Shutter Camera` component (Including the scene view camera) will still simulate exposure.

Using `Volume Profiles`, you can change the physical properties of your camera. If you camera has `usePhysicalProperties` set to `true`, SBTPP will use those settings instead unless explicitly specified by the `Physical Camera` volume.

### Exposure
The `Exposure` volume allows you to easily control the camera exposure.

#### Desired Exposure
This is the desired exposure you wish to achieve using the selected `Exposure Mode`

#### Exposure Mode
* **Do Nothing**
  * SBTPP won't do anything to reach the desired exposure. The exposure will be calculated with the current physical settings as they are.
* **Override ISO**
  * SBTPP will override the current ISO value to reach the desired exposure, leaving the aperture and shutter speed as they are.
* **Override Aperture**
  * SBTPP will override the current aperture value to reach the desired exposure, leaving the ISO and shutter speed as they are.
* **Override Shutter Speed**
  * SBTPP will override the current shutter speed value to reach the desired exposure, leaving the ISO and shutter speed as they are.

### Physical Camera

#### Camera Body
Settings for your camera's light sensor

* ISO
  * The camera's light sensor sensitivity.
  * Affects:
    * Exposure
* Shutter Speed
  * The ammount of time (seconds) the camera's sensor is capturing light.
  * Affects:
    * Temporal Accumulation Factor
    * Exposure
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
    * Exposure
    * Depth of Field
    * Bloom and lens flares

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

### Additional Settings
Extra settings to control potential undesired effects.

* Max Aperture Jitter Allowed
  * The maximum intensity allowed for aperture jitter. A value of 0 disables Depth of Field completely
* Max Pixel Jitter Allowed
  * The maximum intensity allowed for TAA pixel jitter, a value of 0 disables any anti-aliasing attempt.
* Enable Lens Flare
  * Optional lens flares on top of bloom. Feel free to play around with this value according to your game/app needs and artstyle.