var mainDiv;
var trackCanvas;
var heatMapCanvas;
var trackMapCanvas;

var trackCtx;
var heatMapCtx;
var trackMapCtx;

var width=10;
var height=10;

//...........................................................main
function main() {
    mainDiv = document.getElementById("mainDiv");
    trackCanvas = document.getElementById("trackCanvas");
    heatMapCanvas = document.getElementById("heatMapCanvas");
    trackMapCanvas = document.getElementById("trackMapCanvas");

    trackCtx = trackCanvas.getContext("2d");
    heatMapCtx = heatMapCanvas.getContext("2d");
    trackMapCtx = trackMapCanvas.getContext("2d");

    updateLayout();

    $(window).resize(function () {
        updateLayout();
    });

    $('#loadImageButton').click(function(){
        $('.mapImage').attr('src', $('#imageLinkInput').val());
    });

    $('#pauseCheckBox').prop('checked', false);

    $('#pauseCheckBox').change(function() {
        //console.log(this.checked);
        paused=this.checked;
    });
}

function updateLayout() {
    width=$("#mainDiv").innerWidth();
    height=$("#mainDiv").innerHeight();

    trackCanvas.width=width;
    trackCanvas.height=height;
}
//...........................................................socket.io
var socket=io();
//...........................................................eye-tracking

var paused=false;
socket.on("eyeData", function(data){
    if (paused) return;

    var mx=(data.GazeL[0]+data.GazeR[0])*0.5;
    var my=(data.GazeL[1]+data.GazeR[1])*0.5;


    data.ncoords=getElementNCoordsFromGaze(document.getElementById("mainDiv"), mx, my);
    data.time=new Date();

    hmap.addTrack(data);


    redraw();
});

function getElementNCoordsFromGaze(trackElement, nx, ny) {
    //screen size in pixels [maybe points in MAC]
    var screenW=window.screen.width;
    var screenH=window.screen.height;

    //window in screen pixels
    var wx=window.screenLeft;
    var wy=window.screenTop;

    var ww=window.outerWidth;
    var wh=window.outerHeight;

    //client area in screen pixels
    var cw=window.innerWidth;
    var ch=window.innerHeight;

    var cx=wx+(ww-cw)/2; //assuming no side bars
    var cy=wy+(wh-ch); //assuming no bottom status bar or debugging console open

    //canvas properties
    var trackRect = trackElement.getBoundingClientRect();
    var elx=trackRect.left+cx;
    var ely=trackRect.top+cy;
    var elw=trackRect.width;
    var elh=trackRect.height;

    var xx=nx*screenW;
    var yy=ny*screenH;

    var ex=(xx-elx)/elw;
    var ey=(yy-ely)/elh;

    return {x:ex, y:ey};
}

function redraw() {
    trackMapCtx.clearRect(0,0,trackMapCanvas.width, trackMapCanvas.height);
    hmap.drawTracks(trackMapCtx);

    heatMapCtx.clearRect(0,0,heatMapCanvas.width, heatMapCanvas.height);
    hmap.drawMap(heatMapCtx);

   // trackCtx.clearRect(0,0,trackCanvas.width, trackCanvas.height);
   // hmap.drawMap(trackCtx);
}

//..........................................heatMap

var hmap=new HMap(30,20);

function HPixel(i, j, x, y, hmap) {
    this.map=hmap;
    this.i=i;
    this.j=j;
    this.x=x;
    this.y=y;

    this.ng=[];

    this.tracks=0;
}

function HMap(rx, ry) {
    this.rx=rx;
    this.ry=ry;

    this.dx=1.0/rx;
    this.dy=1.0/ry;

    this.pixels=[];
    for(var j=0; j<ry; ++j) {
        for(var i=0; i<rx; ++i) {
            var p=new HPixel(i,j,i*this.dx, j*this.dy, this);
            this.pixels.push(p);
        }
    }

    var k=0;
    for(var j=0; j<ry; ++j) {
        for(var i=0; i<rx; ++i) {
            var p=this.pixels[k];

            if (i) p.ng.push(this.pixels[k-1]);
            else p.ng.push(p);

            if (i!=rx-1) p.ng.push(this.pixels[k+1]);
            else p.ng.push(p);

            if (j) p.ng.push(this.pixels[k-rx]);
            else p.ng.push(p);

            if (j!=ry-1) p.ng.push(this.pixels[k+rx]);
            else p.ng.push(p);

            k++;
        }
    }

    this.maxcount=0;

    this.eyeTracks=[];
}

HMap.prototype.addTrack=function(data) {
    this.eyeTracks.push(data);

    if (this.eyeTracks.length > 1000) {
        this.eyeTracks.splice(0, 1);
    }

    var i=Math.floor(data.ncoords.x*this.rx);
    var j=Math.floor(data.ncoords.y*this.ry);

    if (i>=0 && i<this.rx && j>=0 && j<this.ry) {
        var p=this.pixels[j*this.rx+i];
        p.tracks+=1.0;

        if (p.tracks>this.maxcount)
            this.maxcount= p.tracks;
    }
}

HMap.prototype.drawTracks=function(context) {
    if (this.eyeTracks.length<2) return;

    context.strokeStyle="#ff0000";
    context.globalAlpha=0.5;
    context.lineWidth=2;

    var w=context.canvas.width;
    var h=context.canvas.height;

    context.beginPath();
    context.moveTo(this.eyeTracks[0].ncoords.x*w , this.eyeTracks[0].ncoords.y*h);
    for(var i in this.eyeTracks) {
        context.lineTo(this.eyeTracks[i].ncoords.x*w , this.eyeTracks[i].ncoords.y*h);
    }
    context.stroke();
}

HMap.prototype.drawMap=function(context) {
    if (this.maxcount==0) return;

    context.strokeStyle="#ff0000";
    context.globalAlpha=0.5;

    var w=context.canvas.width;
    var h=context.canvas.height;

    for(var i in this.pixels) {
        var p=this.pixels[i];
        var d= (1.0-(p.tracks/this.maxcount))*225.0;
        context.fillStyle=HSL(d, 100, 50);
        context.fillRect(p.x*w, p.y*h, this.dx*w, this.dy*h);
    }
}

function HSL(h,s,l) {
    return 'hsl('+h+','+s+'%,'+l+'%)';
}

function RGB(r, g, b) {
    return 'rgb('+r+','+g+','+b+')';
}