//..................initialize required modules
var express = require('express'); //import express library to help with typical server funcitonality [serving files, parsing parameters etc...]
var app = express();                //create the express app
var http = require('http');

app.use(express.static(__dirname + '/public'));

var server = app.listen(process.env.PORT || 6789, function () {
    console.log('Listening on port %d', server.address().port); //this line is executed when the server successfully goes into listening mode
});


//................................Socket.io
var io = require('socket.io').listen(server);

io.sockets.on('connection', function (socket) {
    console.log("socket.io: new incoming connection");

    socket.on('disconnect', function () {
        console.log("socket.io: connection lost");
    });

    socket.on('eyeData', function (data) {

        //console.log(data);
        io.sockets.emit('eyeData', data);
    });

});