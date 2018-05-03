#Networking Module (Experimental)

**UNET Example scene depends on the Interaction Engine module.**

This module contains useful scripts for sending Leap Hand data over the network and streaming it to a peer in a way that is light-weight but still expressive. By default, this is accomplished using the VectorHand encoding, which is technically lossy but retains a lot of visual acuity while also being far, far cheaper than a fully serialized Leap Hand. VectorHand is already available in our Core Assets in the Leap.Unity.Encoding namespace.

The included example intentionally avoids polished solutions that solve general "networking", since we expect many practical applications to construct their own data transit and peer connection solutions or utilize pre-existing Asset Store assets.