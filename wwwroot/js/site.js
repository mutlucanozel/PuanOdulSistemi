// Shared particle background initializer for auth pages.
window.initAuthParticles = function initAuthParticles(canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const ctx = canvas.getContext("2d", { alpha: true });
    if (!ctx) return;

    const palette = [
        "rgba(47, 77, 162, 0.62)",
        "rgba(92, 123, 231, 0.56)",
        "rgba(202, 215, 255, 0.7)",
        "rgba(255, 255, 255, 0.52)"
    ];

    const pointer = { x: null, y: null, tx: null, ty: null };
    const dpr = Math.min(window.devicePixelRatio || 1, 1.8);
    const baseCount = 280;
    let particles = [];
    let width = 0;
    let height = 0;
    let rafId = 0;

    function resize() {
        width = window.innerWidth;
        height = window.innerHeight;
        canvas.width = width * dpr;
        canvas.height = height * dpr;
        canvas.style.width = width + "px";
        canvas.style.height = height + "px";
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        buildParticles();
    }

    function buildParticles() {
        const density = Math.max(120, Math.round(baseCount * (width * height) / (1920 * 1080)));
        particles = Array.from({ length: density }, createParticle);
    }

    function createParticle() {
        const angle = Math.random() * Math.PI * 2;
        const layer = Math.random();
        const speed = 0.11 + layer * 0.37;
        const minR = 0.6;
        const maxR = 4.6;
        const radius = Math.pow(Math.random(), 1.75) * (maxR - minR) + minR;
        return {
            x: Math.random() * width,
            y: Math.random() * height,
            vx: Math.cos(angle) * speed,
            vy: Math.sin(angle) * speed,
            radius: radius,
            baseRadius: radius,
            layer: layer,
            color: palette[Math.floor(Math.random() * palette.length)],
            drift: {
                freq: Math.random() * 0.002 + 0.0006,
                phase: Math.random() * Math.PI * 2
            }
        };
    }

    function draw(time) {
        ctx.clearRect(0, 0, width, height);
        const t = time * 0.0016;
        const pointerActive = pointer.x !== null && pointer.y !== null;

        if (pointerActive) {
            pointer.x += (pointer.tx - pointer.x) * 0.14;
            pointer.y += (pointer.ty - pointer.y) * 0.14;
        }

        for (let i = 0; i < particles.length; i++) {
            const p = particles[i];
            p.x += p.vx + Math.cos(t * p.drift.freq + p.drift.phase) * (0.5 + p.layer);
            p.y += p.vy + Math.sin(t * p.drift.freq + p.drift.phase) * (0.5 + p.layer);

            if (pointerActive) {
                const dx = p.x - pointer.x;
                const dy = p.y - pointer.y;
                const dist = Math.sqrt(dx * dx + dy * dy) || 0.001;
                const influence = Math.max(0, 1 - dist / 220);
                const force = influence * (0.8 + p.layer);
                p.vx += (dx / dist) * force * 0.05;
                p.vy += (dy / dist) * force * 0.05;
                p.radius = p.baseRadius + influence * 2.4;
            } else {
                p.radius += (p.baseRadius - p.radius) * 0.05;
            }

            p.vx *= 0.985;
            p.vy *= 0.985;

            if (p.x < -60) p.x = width + 60;
            if (p.x > width + 60) p.x = -60;
            if (p.y < -60) p.y = height + 60;
            if (p.y > height + 60) p.y = -60;
        }

        const linkThreshold = Math.min(195, Math.max(width, height) * 0.2);
        ctx.globalCompositeOperation = "lighter";
        ctx.lineWidth = 1;

        for (let i = 0; i < particles.length; i++) {
            const p1 = particles[i];
            for (let j = i + 1; j < particles.length; j++) {
                const p2 = particles[j];
                const dx = p1.x - p2.x;
                const dy = p1.y - p2.y;
                const dist = Math.sqrt(dx * dx + dy * dy);
                if (dist < linkThreshold) {
                    const alpha = (1 - dist / linkThreshold) * 0.28;
                    ctx.strokeStyle = "rgba(112, 146, 255, " + alpha + ")";
                    ctx.beginPath();
                    ctx.moveTo(p1.x, p1.y);
                    ctx.lineTo(p2.x, p2.y);
                    ctx.stroke();
                }
            }
        }

        for (let i = 0; i < particles.length; i++) {
            const p = particles[i];
            const gradient = ctx.createRadialGradient(p.x, p.y, 0, p.x, p.y, p.radius * 2.2);
            gradient.addColorStop(0, "rgba(255,255,255,0.88)");
            gradient.addColorStop(1, p.color);
            ctx.fillStyle = gradient;
            ctx.beginPath();
            ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
            ctx.fill();
        }

        ctx.globalCompositeOperation = "source-over";
        rafId = requestAnimationFrame(draw);
    }

    function onPointerMove(evt) {
        pointer.tx = evt.clientX;
        pointer.ty = evt.clientY;
        if (pointer.x === null || pointer.y === null) {
            pointer.x = pointer.tx;
            pointer.y = pointer.ty;
        }
    }

    function onPointerLeave() {
        pointer.x = null;
        pointer.y = null;
        pointer.tx = null;
        pointer.ty = null;
    }

    function destroy() {
        cancelAnimationFrame(rafId);
        window.removeEventListener("resize", resize);
        window.removeEventListener("pointermove", onPointerMove);
        window.removeEventListener("pointerleave", onPointerLeave);
    }

    window.addEventListener("resize", resize);
    window.addEventListener("pointermove", onPointerMove);
    window.addEventListener("pointerleave", onPointerLeave);
    window.addEventListener("beforeunload", destroy, { once: true });

    resize();
    rafId = requestAnimationFrame(draw);
};

// GSAP-powered card and form animation for login/register screens.
window.initAuthCardFx = function initAuthCardFx() {
    const card = document.querySelector(".login-min-card");
    if (!card || card.dataset.fxReady === "1") return;
    card.dataset.fxReady = "1";

    const tabs = card.querySelectorAll(".auth-tab");
    const header = card.querySelector(".auth-card-head");
    const fields = card.querySelectorAll(".auth-field");
    const submit = card.querySelector(".login-cool-btn");

    tabs.forEach((tab) => {
        tab.style.opacity = "1";
        tab.style.visibility = "visible";
        tab.style.transform = "none";
    });

    if (window.gsap) {
        const tl = window.gsap.timeline({ defaults: { ease: "power2.out" } });
        tl.from(card, { y: 26, opacity: 0, duration: 0.55 })
          .from(tabs, { y: 10, duration: 0.28, stagger: 0.06 }, "-=0.28");

        if (header) {
            tl.from(header, { y: 12, opacity: 0, duration: 0.3 }, "-=0.18");
        }

        if (fields.length > 0) {
            tl.from(fields, { y: 10, opacity: 0, duration: 0.28, stagger: 0.07 }, "-=0.2");
        }

        if (submit) {
            tl.from(submit, { y: 10, opacity: 0, duration: 0.24 }, "-=0.1");
        }
    }

    fields.forEach((field) => {
        const input = field.querySelector("input");
        if (!input) return;

        input.addEventListener("focus", function () {
            field.classList.add("focused");
        });
        input.addEventListener("blur", function () {
            field.classList.remove("focused");
        });
    });
};
