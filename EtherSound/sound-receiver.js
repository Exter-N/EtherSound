#!/usr/bin/node

const underrunMessage = Buffer.from('sox WARN alsa: under-run\n', 'utf8');

const child = require('child_process');
const dgram = require('dgram');

function calculateBufferSize(samplerate, channels) {
    let size = Math.ceil(samplerate * channels / 30);

    return 1 << (32 - Math.clz32(size));
}

const soxMap = new Map();

class Sox {
    constructor(id, samplerate, channels) {
        this.id = id;
        this.lastActive = Date.now();
        this.underrunPosition = 0;
        this.child = child.spawn('sox', [
            '--buffer', '' + calculateBufferSize(samplerate, channels),
            '-q',
            '-r', '' + samplerate,
            '-b', '32',
            '-e', 'floating',
            '-c', '' + channels,
            '-t', '.f32', '-',
            '-d',
        ], {
            stdio: [ 'pipe', 1, 'pipe' ],
        });
        this.child.on('exit', (code, signal) => this.onExit(code, signal));
        this.child.stderr.on('data', chunk => this.onStderrData(chunk));
        this.pid = this.child.pid;
    }
    play(buffer) {
        this.lastActive = Date.now();
        this.child.stdin.write(buffer);
    }
    end() {
        this.child.stdin.end();
        this.child.kill();
    }
    onExit(code, signal) {
        if (this === soxMap.get(this.id)) {
            console.error('Reaping exited Sox for stream ' + this.id + ' (' + this.pid + ')');
            soxMap.delete(this.id);
        }
    }
    onStderrData(chunk) {
        let start = 0;
        for (let i = 0; i < chunk.length; ++i) {
            if (chunk[i] === underrunMessage[this.underrunPosition]) {
                if (this.underrunPosition === 0 && i > start) {
                    process.stderr.write(chunk.slice(start, i));
                    start = i;
                }
                ++this.underrunPosition;
                if (this.underrunPosition === underrunMessage.length) {
                    this.underrunPosition = 0;
                    start = i + 1;
                }
            } else {
                if (this.underrunPosition > 0) {
                    process.stderr.write(underrunMessage.slice(0, this.underrunPosition));
                    this.underrunPosition = 0;
                    start = i;
                }
            }
        }
        if (this.underrunPosition === 0 && start < chunk.length) {
            process.stderr.write(chunk.slice(start));
        }
    }
}

setInterval(() => {
    const now = Date.now();
    const expiredIds = [];
    for (const [ id, sox ] of soxMap.entries()) {
        if (now - sox.lastActive > 120000) {
            console.error('Cleaning up Sox for idle stream ' + id + ' (PID ' + sox.pid + ')');
            sox.end();
            expiredIds.push(id);
        }
    }
    for (const id of expiredIds) {
        soxMap.delete(id);
    }
}, 15000);

function decodeSamplerate(header) {
    const base = (header & 128) ? 44100 : 48000;

    return (header & 127) * base;
}

const sock = dgram.createSocket('udp4');

sock.on('listening', () => {
    const address = sock.address();
    console.error('Listening on ' + address.address + ':' + address.port);
});

sock.on('message', (data, rinfo) => {
    if (data[1] != 32) {
        return;
    }
    const soxId = rinfo.address + ':' + rinfo.port + ':' + data[0] + ':' + data[2];
    let sox = soxMap.get(soxId);
    if (null == sox) {
        sox = new Sox(soxId, decodeSamplerate(data[0]), data[2]);
        console.error('Spawning Sox for stream ' + soxId + ' (PID ' + sox.pid + ')');
        soxMap.set(soxId, sox);
    }
    sox.play(data.slice(5));
});

sock.bind(4010, () => {
    sock.addMembership('239.255.77.77');
});