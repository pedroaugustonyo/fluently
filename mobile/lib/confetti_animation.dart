import 'dart:math' as math;

import 'package:flutter/material.dart';

const confettiAnimationDuration = Duration(milliseconds: 1100);

class ConfettiAnimation extends StatelessWidget {
  const ConfettiAnimation({super.key, required this.progress});

  final double progress;

  @override
  Widget build(BuildContext context) => Opacity(
    opacity: (1 - progress).clamp(0, 1),
    child: CustomPaint(painter: _ConfettiPainter(progress)),
  );
}

class _ConfettiPainter extends CustomPainter {
  _ConfettiPainter(this.progress);

  final double progress;

  @override
  void paint(Canvas canvas, Size size) {
    final random = math.Random(24);
    const colors = [
      Color(0xFF00A887),
      Color(0xFFFFC857),
      Color(0xFFFF7D6B),
      Color(0xFF7188E8),
    ];

    for (var index = 0; index < 42; index++) {
      final startX = random.nextDouble() * size.width;
      final drift = (random.nextDouble() - .5) * 120;
      final x = startX + drift * progress;
      final y = -20 + (size.height * .68 * progress) + random.nextDouble() * 85;
      final rotation = progress * math.pi * (2 + random.nextDouble() * 3);
      final confettiSize = 5 + random.nextDouble() * 5;
      final paint = Paint()..color = colors[index % colors.length];

      canvas.save();
      canvas.translate(x, y);
      canvas.rotate(rotation);
      canvas.drawRRect(
        RRect.fromRectAndRadius(
          Rect.fromCenter(
            center: Offset.zero,
            width: confettiSize,
            height: confettiSize * 1.7,
          ),
          const Radius.circular(2),
        ),
        paint,
      );
      canvas.restore();
    }
  }

  @override
  bool shouldRepaint(covariant _ConfettiPainter oldDelegate) =>
      oldDelegate.progress != progress;
}
