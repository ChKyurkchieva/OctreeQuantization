**Project Overview**

This project implements the Octree color quantization algorithm, a classic and efficient method for reducing the number of colors in an image (also known as color quantization).
The algorithm processes an input image as follows:

  1. Constructs an octree representation of the color space based on the image's pixels
  2. Reduces the number of colors by pruning the octree until the desired palette size is reached
  3. Maps each original pixel color to the nearest color in a custom RGBA palette supplied by the user in CSV format

The result is a quantized image that preserves visual quality while significantly decreasing the colors.

### Example Results 

| Original                        | Quantized (custom palette)             |
|---------------------------------|----------------------------------------|
| ![Original](OctreeQuantization/Images/VikingGirl.png) | ![Quantized](OctreeQuantization/Images/VikingGirlMyOctree.png)    |
