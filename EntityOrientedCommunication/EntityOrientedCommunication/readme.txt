Upgrade to EntityOrientedCommunication 1.1.3

NewtonSoft.Json users are recommended to customize your own [Serializer] to 
keep the message transmission correct. The concrete operations are as following.

1. Customize your own serializer class which implements the 
'EntityOrientedCommunication.ISerializer' interface.

2. Instantiate the serializer you customized and set it to the property value of 
static configuration class 'EntityOrientedCommunication.Configuration.Serializer'.