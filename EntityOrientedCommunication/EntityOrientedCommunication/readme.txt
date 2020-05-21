Upgrade from EntityOrientedCommunication from 1.1.2 to 1.1.4

NewtonSoft.Json users are recommended to customize your own [Serializer] to 
keep the message transmission correct. The concrete operations are as following.

1. Customize your own serializer class which implements the 
'EntityOrientedCommunication.ISerializer' interface.

2. Instantiate the serializer you customized and set it to the property named 'Serializer' of 
static configuration class 'EntityOrientedCommunication.Configuration'.