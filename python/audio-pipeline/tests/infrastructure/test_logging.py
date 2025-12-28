import os
import pytest
import logging
from src.infrastructure.logging import StandardLogger


def test_standard_logger_creates_file_handler(tmp_path):
    """Hits the file handler creation and directory making logic. 📝✨"""
    log_file = tmp_path / "subdir" / "test.log"
    logger = StandardLogger("TestLogger", log_file=str(log_file))
    
    logger.info("File log test")
    
    assert log_file.exists()
    with open(log_file, "r") as f:
        assert "File log test" in f.read()


def test_standard_logger_supports_different_log_levels(caplog):
    """Verifies that the logger respects the level setting. 📊✨"""
    logger = StandardLogger("LevelLogger", level=logging.ERROR)
    
    with caplog.at_level(logging.INFO):
        logger.info("Should not see this")
        logger.error("Must see this")
        
    assert "Should not see this" not in caplog.text
    assert "Must see this" in caplog.text