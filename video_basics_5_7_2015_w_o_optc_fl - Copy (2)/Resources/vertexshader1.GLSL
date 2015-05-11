varying vec3 wposition;
varying vec3 wnormal;
varying vec3 normal;
varying vec3 vertex_to_light_vector;
varying vec2 texture_coordinate;
varying vec4 color;
varying vec4 posL;

void main()
{
	posL=gl_ModelViewProjectionMatrix * gl_Vertex;
	gl_Position = posL;

	vec3 vp=(gl_ModelViewMatrix*gl_Vertex).xyz;

	float dvp=length(vp.xy);
	vp=normalize(vp);

	wposition=gl_Vertex.xyz-vp.xyz*exp(-0.1*dvp*dvp)*5.0;
	wnormal = gl_Normal;
	normal = gl_NormalMatrix * gl_Normal;

	gl_Position=gl_ModelViewProjectionMatrix*vec4(wposition, 1.0);

   // Transforming The Vertex Position To ModelView-Space
    vec4 vertex_in_modelview_space = gl_ModelViewMatrix * gl_Vertex;
 
    // Calculating The Vector From The Vertex Position To The Light Position
    vertex_to_light_vector = vec3(gl_LightSource[0].position-vertex_in_modelview_space);
	//vertex_to_light_vector = vec3(gl_LightSource[0].position-wposition);

	


	color=gl_Color;
	texture_coordinate = vec2(gl_MultiTexCoord0);
   
} 
